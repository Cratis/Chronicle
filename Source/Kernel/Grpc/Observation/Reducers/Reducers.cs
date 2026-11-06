// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Events;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ObserverType = Cratis.Chronicle.Concepts.Observation.ObserverType;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for observing actual events as they are made available.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from <see cref="ExpandoObject"/>.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for how often to check the subscription of a connected client.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="registrationGate">The silo-wide gate for observer registrations.</param>
internal sealed class Reducers(
    IGrainFactory grainFactory,
    IReducerMediator reducerMediator,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<Reducers> activitySource,
    IOptions<ChronicleOptions> options,
    ILogger<Reducers> logger,
    IObserverRegistrationGate registrationGate) : IReducers
{
    /// <summary>
    /// The size, in characters, of a reducer's serialized state from which each reduce is reported as expensive.
    /// </summary>
    /// <remarks>
    /// Every reduce sends the whole current state to the client, so the cost of a reduce grows with the read model.
    /// </remarks>
    internal const int LargeStateWarningThreshold = 8 * 1024 * 1024;

    /// <summary>
    /// The most partitions per stream the kernel remembers a returned state fingerprint for. The client side cache is larger
    /// so the kernel never assumes the client holds a state the client has already evicted.
    /// </summary>
    internal const int MaxCachedStateFingerprints = 50_000;

    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingDeliveries = new PendingDeliveries<ReducerSubscriberResult>();
        IReducer? clientObserver = null;
        var stateFingerprints = new ReducerStateFingerprints(MaxCachedStateFingerprints);

        // The read model definition will be populated once the reducer is registered.
        var model = new ReadModelDefinition(
            ReadModelIdentifier.NotSet,
            ReadModelContainerName.NotSet,
            ReadModelDisplayName.NotSet,
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);

        var messagesSubscription = messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterReducer register:
                    if (SystemStoreGuard.RegistrationRefusal(new(register.EventStore, register.Namespace, register.Reducer.ReducerId, register.Reducer.EventSequenceId, register.Reducer.ReadModel, register.ConnectionId)) is { } refusal)
                    {
                        registrationTcs.TrySetException(refusal);
                        break;
                    }

                    logger.Registering(
                        register.Reducer.ReducerId,
                        register.EventStore,
                        register.Namespace,
                        register.Reducer.EventSequenceId,
                        register.ConnectionId);

                    stateFingerprints.Enabled = register.StateCaching?.Enabled == true;
                    registrationTcs.TrySetResult(register);
                    break;

                case ReducerResult result:
                    foreach (var (partition, subscriberResult) in ToSubscriberResults(result, model))
                    {
                        RememberReturnedState(stateFingerprints, partition, subscriberResult);
                        if (!pendingDeliveries.TryComplete(result.DeliveryId, partition, subscriberResult))
                        {
                            logger.UnknownDeliveryResult(result.DeliveryId, partition);
                        }
                    }

                    break;
            }
        });

        var connectionId = ConnectionId.NotSet;
        var observerId = ObserverId.Unspecified;
        var eventStoreName = EventStoreName.NotSet;
        var namespaceName = EventStoreNamespaceName.NotSet;
        IObserver<ReduceOperationMessage>? observableObserver = null;
        (ReducerEventsObserver? deliver, ReducerReplayObserver? replay) = (null, null);

        var observable = Observable.Create<ReduceOperationMessage>(
            async (observer, cancellationToken) =>
            {
                observableObserver = observer;
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationToken);
                try
                {
                    var registration = await registrationTcs.Task.WaitAsync(cancellation.Token);
                    connectionId = registration.ConnectionId;
                    observerId = registration.Reducer.ReducerId;
                    eventStoreName = registration.EventStore;
                    namespaceName = registration.Namespace;

                    var reducerDefinition = registration.Reducer.ToChronicle();
                    using (await registrationGate.Enter(cancellation.Token))
                    {
                        try
                        {
                            model = await grainFactory.GetReadModel(registration.Reducer.ReadModel, registration.EventStore).GetDefinition();
                            ValidateReadModelDefinition(registration, model);
                        }
                        catch (Exception exception)
                        {
                            throw CreateRegistrationFailure(registration, exception);
                        }

                        logger.Subscribing(
                            registration.Reducer.ReducerId,
                            registration.EventStore,
                            registration.Namespace,
                            registration.Reducer.EventSequenceId,
                            registration.ConnectionId);

                        var key = new ConnectedObserverKey(
                            registration.Reducer.ReducerId,
                            registration.EventStore,
                            registration.Namespace,
                            registration.Reducer.EventSequenceId,
                            registration.ConnectionId);

                        var deliversPages = registration.PageDelivery?.Enabled == true;
                        deliver = (reduceOperation, tcs) =>
                        {
                            var deliveryId = pendingDeliveries.Add(reduceOperation.Partition, tcs);

                            var partition = reduceOperation.Partition.Value.ToString()!;

                            // A state the client already holds is not serialized at all: the client returned it on its previous reduce.
                            var initialStateUnchanged = IsInitialStateUnchanged(stateFingerprints, partition, reduceOperation.InitialState);

                            // Compact JSON: JsonNode.ToString() writes indented JSON, which inflates every reduce's state by the whitespace.
                            var initialState = reduceOperation.InitialState is null || initialStateUnchanged ? null : expandoObjectConverter.ToJsonObject(reduceOperation.InitialState, model.GetSchemaForLatestGeneration()).ToJsonString(jsonSerializerOptions);
                            if (initialState?.Length > LargeStateWarningThreshold)
                            {
                                logger.LargeInitialState(observerId, reduceOperation.Partition.ToString()!, initialState.Length);
                            }
                            var events = reduceOperation.Events.Select(_ => _.ToContract(jsonSerializerOptions)).ToArray();
                            var message = new ReduceOperationMessage()
                            {
                                DeliveryId = deliveryId,
                                DeliverWithinMilliseconds = DeliveryDeadlines.ToMilliseconds(reduceOperation.DeliverWithin),
                                Origin = reduceOperation.Origin.ToContract()
                            };

                            if (deliversPages)
                            {
                                message.Partitions = [new ReducerPartition { Partition = partition, InitialState = initialState, InitialStateUnchanged = initialStateUnchanged, Events = events }];
                            }
                            else
                            {
                                message.Partition = partition;
                                message.InitialState = initialState;
                                message.InitialStateUnchanged = initialStateUnchanged;
                                message.Events = events;
                            }

                            observer.OnNext(message);
                        };
                        replay = (replayState, partition) =>
                        {
                            ForgetState(stateFingerprints, partition);
                            observer.OnNext(new ReduceOperationMessage { Partition = partition, ReplayState = (Contracts.Observation.ReplayState)replayState });
                        };

                        reducerMediator.Subscribe(
                            registration.Reducer.ReducerId,
                            registration.ConnectionId,
                            registration.EventStore,
                            registration.Namespace,
                            deliver);

                        reducerMediator.SubscribeReplayNotifications(
                            registration.Reducer.ReducerId,
                            registration.ConnectionId,
                            registration.EventStore,
                            registration.Namespace,
                            replay);

                        // The parent is the long-lived Observe request, which the server deliberately does not trace.
                        var requestActivity = Activity.Current;
                        Activity.Current = null;
                        try
                        {
                            using var span = activitySource.Register();
                            span?.Activity?.Tag(key, ObserverType.Reducer);

                            clientObserver = grainFactory.GetGrain<IReducer>(key);
                            await clientObserver.SetDefinitionAndSubscribe(reducerDefinition);
                        }
                        catch (Exception exception)
                        {
                            throw CreateRegistrationFailure(registration, exception);
                        }
                        finally
                        {
                            Activity.Current = requestActivity;
                        }
                    }

                    await ClientSubscriptionKeeper.KeepWhileConnected(
                        () => clientObserver!.KeepSubscription(reducerDefinition),
                        TimeSpan.FromSeconds(options.Value.ConnectedClients.ReviseIntervalSeconds),
                        registration.ConnectionId,
                        logger,
                        cancellation.Token);

                    logger.Disconnected(
                        registration.Reducer.ReducerId,
                        registration.EventStore,
                        registration.Namespace,
                        registration.Reducer.EventSequenceId,
                        registration.ConnectionId);
                }
                catch (OperationCanceledException)
                {
                    logger.DisconnectedUngracefully(observerId, connectionId);
                    clientObserver = null;
                }
                catch (Exception ex)
                {
                    logger.Disengage(observerId, connectionId, ex);
                    context.RecordFailure(ex);
                    observer.OnError(ex);
                }
                finally
                {
                    messagesSubscription.Dispose();
                    var ownsRegistration = DisconnectStream((observerId, connectionId, eventStoreName, namespaceName), deliver, replay);
                    pendingDeliveries.Disconnect(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject()));
                    await ReleaseRegistration(clientObserver, observerId, connectionId, ownsRegistration);
                    clientObserver = null;
                }
            });

        CancellationTokenRegistration? register = null;

        register = context.CancellationToken.Register(() =>
        {
            logger.ObserverStreamDisconnected(observerId, connectionId);
            stateFingerprints.Clear();
            pendingDeliveries.Disconnect(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject()));
            observableObserver?.OnCompleted();
            DisconnectStream((observerId, connectionId, eventStoreName, namespaceName), deliver, replay);

            register?.Dispose();
        });

        return observable;
    }

    static bool IsInitialStateUnchanged(ReducerStateFingerprints stateFingerprints, string partition, ExpandoObject? initialState)
    {
        if (!stateFingerprints.Enabled || initialState is null || !stateFingerprints.Has(partition))
        {
            return false;
        }

        return ReadModelStateFingerprint.TryCompute(initialState, out var fingerprint) && stateFingerprints.IsUnchanged(partition, fingerprint);
    }

    static void RememberReturnedState(ReducerStateFingerprints stateFingerprints, string partition, ReducerSubscriberResult result)
    {
        if (!stateFingerprints.Enabled)
        {
            return;
        }

        // Only a handled partition with a state is remembered; anything else makes the next delivery carry the full state.
        if (result.ObserverResult.State == ObserverSubscriberState.Ok &&
            result.ReadModelState is not null &&
            ReadModelStateFingerprint.TryCompute(result.ReadModelState, out var fingerprint))
        {
            stateFingerprints.Set(partition, fingerprint);
            return;
        }

        stateFingerprints.Remove(partition);
    }

    static void ForgetState(ReducerStateFingerprints stateFingerprints, string? partition)
    {
        if (string.IsNullOrEmpty(partition))
        {
            stateFingerprints.Clear();
            return;
        }

        stateFingerprints.Remove(partition);
    }

    static ReducerRegistrationFailed CreateRegistrationFailure(RegisterReducer registration, Exception exception)
    {
        if (exception is ReducerRegistrationFailed reducerRegistrationFailed)
        {
            return reducerRegistrationFailed;
        }

        var rootCause = exception is AggregateException aggregateException ? aggregateException.Flatten().InnerExceptions.FirstOrDefault() ?? exception : exception;
        return new ReducerRegistrationFailed(
            registration.Reducer.ReducerId,
            registration.EventStore,
            registration.Namespace,
            registration.Reducer.EventSequenceId,
            registration.ConnectionId,
            rootCause);
    }

    static void ValidateReadModelDefinition(RegisterReducer registration, ReadModelDefinition readModel)
    {
        var schema = readModel.GetSchemaForLatestGeneration();

        if (schema.HasKeyProperty())
        {
            var keyPropertyName = schema.GetKeyProperty().Name;
            var keyPropertyPath = new PropertyPath(keyPropertyName);
            if (schema.GetSchemaPropertyForPropertyPath(keyPropertyPath) is null)
            {
                throw CreateRegistrationFailure(
                    registration,
                    new InvalidReadModelDefinitionForReducer(readModel.Identifier, $"The key property path '{keyPropertyPath.Path}' does not exist in the read model schema."));
            }

            return;
        }

        // When no explicit key property is declared, validate that there are properties at all
        // so that the key can be inferred. The MongoDBConverter handles the case where the
        // heuristic key name doesn't exist in the schema gracefully.
        var likelyKeyPropertyName = schema.GetLikelyKeyPropertyName();
        if (string.IsNullOrWhiteSpace(likelyKeyPropertyName))
        {
            throw CreateRegistrationFailure(
                registration,
                new InvalidReadModelDefinitionForReducer(readModel.Identifier, "No key property could be inferred from the read model schema."));
        }
    }

    bool DisconnectStream(
        (ObserverId ObserverId, ConnectionId ConnectionId, EventStoreName EventStore, EventStoreNamespaceName Namespace) key,
        ReducerEventsObserver? deliver,
        ReducerReplayObserver? replay) =>
        deliver is null || replay is null || reducerMediator.Disconnected(key.ObserverId, key.ConnectionId, key.EventStore, key.Namespace, deliver, replay);

    async Task ReleaseRegistration(IReducer? clientObserver, ObserverId observerId, ConnectionId connectionId, bool ownsRegistration)
    {
        if (clientObserver is null)
        {
            return;
        }

        if (ownsRegistration)
        {
            await clientObserver.Unsubscribe();
            return;
        }

        logger.StreamReplacedKeepingSubscription(observerId, connectionId);
    }

    IEnumerable<(string Partition, ReducerSubscriberResult Result)> ToSubscriberResults(ReducerResult result, ReadModelDefinition model) =>
        result.Partitions.Count > 0
            ? result.Partitions.Select(_ => (_.Partition, ToSubscriberResult(result.DeliveryId, _.Partition, () => new ReducerSubscriberResult(_.ToObserverSubscriberResult(TimeProvider.System), ToReadModelState(_.ReadModelState, model)))))
            : [(result.Partition, ToSubscriberResult(result.DeliveryId, result.Partition, () => new ReducerSubscriberResult(
                ObservationResultConverters.ToObserverSubscriberResult(result.State, result.LastSuccessfulObservation, result.ExceptionMessages, result.ExceptionStackTrace, result.RetryAfterMilliseconds, TimeProvider.System),
                ToReadModelState(result.ReadModelState, model))))];

    /// <summary>
    /// Converts a result of the client, failing the delivery when the state it returned cannot be read.
    /// </summary>
    /// <remarks>
    /// Reading the state can fail - on a state too large to materialize it fails with an out of memory. That must
    /// fail the delivery: an exception here would leave the observer waiting for an answer that has already
    /// arrived, until the subscriber timeout, which is minutes.
    /// </remarks>
    /// <param name="deliveryId">The identifier of the delivery.</param>
    /// <param name="partition">The partition the result is for.</param>
    /// <param name="convert">Converts the result.</param>
    /// <returns>The <see cref="ReducerSubscriberResult"/>.</returns>
    ReducerSubscriberResult ToSubscriberResult(ulong deliveryId, string partition, Func<ReducerSubscriberResult> convert)
    {
        try
        {
            return convert();
        }
        catch (Exception exception)
        {
            logger.UnreadableDeliveryResult(deliveryId, partition, exception);
            return new ReducerSubscriberResult(ObserverSubscriberResult.Failed(Concepts.Events.EventSequenceNumber.Unavailable, exception.Message), null);
        }
    }

    ExpandoObject? ToReadModelState(string? readModelState, ReadModelDefinition model) =>
        readModelState is null ? null : expandoObjectConverter.ToExpandoObject(JsonNode.Parse(readModelState)!.AsObject(), model.GetSchemaForLatestGeneration());
}
