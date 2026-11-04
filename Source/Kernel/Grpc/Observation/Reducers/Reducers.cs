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
internal sealed class Reducers(
    IGrainFactory grainFactory,
    IReducerMediator reducerMediator,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<Reducers> activitySource,
    IOptions<ChronicleOptions> options,
    ILogger<Reducers> logger) : IReducers
{
    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingDeliveries = new PendingDeliveries<ReducerSubscriberResult>();
        IReducer? clientObserver = null;

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

                    grainFactory.GetReadModel(register.Reducer.ReadModel, register.EventStore).GetDefinition().ContinueWith(
                        result =>
                        {
                            if (result.IsFaulted)
                            {
                                registrationTcs.TrySetException(CreateRegistrationFailure(register, result.Exception!));
                            }
                            else
                            {
                                model = result.Result;
                                try
                                {
                                    ValidateReadModelDefinition(register, model);
                                    registrationTcs.TrySetResult(register);
                                }
                                catch (Exception exception)
                                {
                                    registrationTcs.TrySetException(CreateRegistrationFailure(register, exception));
                                }
                            }
                        },
                        TaskScheduler.Current);
                    break;

                case ReducerResult result:
                    foreach (var (partition, subscriberResult) in ToSubscriberResults(result, model))
                    {
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
                try
                {
                    var registration = await registrationTcs.Task;
                    connectionId = registration.ConnectionId;
                    observerId = registration.Reducer.ReducerId;
                    eventStoreName = registration.EventStore;
                    namespaceName = registration.Namespace;

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
                        var initialState = reduceOperation.InitialState is null ? null : expandoObjectConverter.ToJsonObject(reduceOperation.InitialState, model.GetSchemaForLatestGeneration()).ToString();
                        var partition = reduceOperation.Partition.Value.ToString()!;
                        var events = reduceOperation.Events.Select(_ => _.ToContract(jsonSerializerOptions)).ToArray();
                        var message = new ReduceOperationMessage()
                        {
                            DeliveryId = deliveryId,
                            DeliverWithinMilliseconds = DeliveryDeadlines.ToMilliseconds(reduceOperation.DeliverWithin),
                            Origin = reduceOperation.Origin.ToContract()
                        };

                        if (deliversPages)
                        {
                            message.Partitions = [new ReducerPartition { Partition = partition, InitialState = initialState, Events = events }];
                        }
                        else
                        {
                            message.Partition = partition;
                            message.InitialState = initialState;
                            message.Events = events;
                        }

                        observer.OnNext(message);
                    };
                    replay = (replayState, partition) => observer.OnNext(new ReduceOperationMessage { Partition = partition, ReplayState = (Contracts.Observation.ReplayState)replayState });

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

                    var reducerDefinition = registration.Reducer.ToChronicle();

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

                    await ClientSubscriptionKeeper.KeepWhileConnected(
                        () => clientObserver!.KeepSubscription(reducerDefinition),
                        TimeSpan.FromSeconds(options.Value.ConnectedClients.ReviseIntervalSeconds),
                        registration.ConnectionId,
                        logger,
                        cancellationToken);

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
            pendingDeliveries.Disconnect(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject()));
            observableObserver?.OnCompleted();
            DisconnectStream((observerId, connectionId, eventStoreName, namespaceName), deliver, replay);

            register?.Dispose();
        });

        return observable;
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
            ? result.Partitions.Select(_ => (_.Partition, new ReducerSubscriberResult(_.ToObserverSubscriberResult(TimeProvider.System), ToReadModelState(_.ReadModelState, model))))
            : [(result.Partition, new ReducerSubscriberResult(
                ObservationResultConverters.ToObserverSubscriberResult(result.State, result.LastSuccessfulObservation, result.ExceptionMessages, result.ExceptionStackTrace, result.RetryAfterMilliseconds, TimeProvider.System),
                ToReadModelState(result.ReadModelState, model)))];

    ExpandoObject? ToReadModelState(string? readModelState, ReadModelDefinition model) =>
        readModelState is null ? null : expandoObjectConverter.ToExpandoObject(JsonNode.Parse(readModelState)!.AsObject(), model.GetSchemaForLatestGeneration());
}
