// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Events;
using Cratis.Collections;
using Microsoft.Extensions.Logging;
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
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class Reducers(
    IGrainFactory grainFactory,
    IReducerMediator reducerMediator,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<Reducers> logger) : IReducers
{
    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentDictionary<EventSourceId, TaskCompletionSource<ReducerSubscriberResult>> reducerResultTcs = [];
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
                    var state = result.State switch
                    {
                        ObservationState.None => ObserverSubscriberState.None,
                        ObservationState.Success => ObserverSubscriberState.Ok,
                        ObservationState.Failed => ObserverSubscriberState.Failed,
                        _ => ObserverSubscriberState.None
                    };

                    var modelResult = (result.ReadModelState is null) ? null :
                        expandoObjectConverter.ToExpandoObject(JsonNode.Parse(result.ReadModelState)!.AsObject(), model.GetSchemaForLatestGeneration());

                    var subscriberResult = new ReducerSubscriberResult(
                        new ObserverSubscriberResult(
                            state,
                            result.LastSuccessfulObservation,
                            result.ExceptionMessages,
                            result.ExceptionStackTrace),
                        modelResult);

                    if (reducerResultTcs.TryGetValue(result.Partition, out var tcs))
                    {
                        tcs.SetResult(subscriberResult);
                        reducerResultTcs.TryRemove(result.Partition, out _);
                    }

                    break;
            }
        });

        var connectionId = ConnectionId.NotSet;
        var observerId = ObserverId.Unspecified;
        var eventStoreName = EventStoreName.NotSet;
        var namespaceName = EventStoreNamespaceName.NotSet;
        IObserver<ReduceOperationMessage>? observableObserver = null;

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

                    reducerMediator.Subscribe(
                        registration.Reducer.ReducerId,
                        registration.ConnectionId,
                        registration.EventStore,
                        registration.Namespace,
                        (reduceOperation, tcs) =>
                        {
                            reducerResultTcs[reduceOperation.Partition] = tcs;
                            var initialState = reduceOperation.InitialState is null ? null : expandoObjectConverter.ToJsonObject(reduceOperation.InitialState, model.GetSchemaForLatestGeneration()).ToString();
                            var message = new ReduceOperationMessage()
                            {
                                Partition = reduceOperation.Partition.Value.ToString()!,
                                InitialState = initialState,
                                Events = reduceOperation.Events.Select(_ => _.ToContract(jsonSerializerOptions)).ToArray()
                            };

                            observer.OnNext(message);
                        });

                    using (Tracing.RegisterObserver(key, ObserverType.Reducer))
                    {
                        var reducerDefinition = registration.Reducer.ToChronicle();

                        clientObserver = grainFactory.GetGrain<IReducer>(key);
                        try
                        {
                            await clientObserver.SetDefinitionAndSubscribe(reducerDefinition);
                        }
                        catch (Exception exception)
                        {
                            throw CreateRegistrationFailure(registration, exception);
                        }
                    }

                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

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
                    observer.OnError(ex);
                }
                finally
                {
                    messagesSubscription.Dispose();
                    reducerMediator.Disconnected(observerId, connectionId, eventStoreName, namespaceName);
                    reducerResultTcs.Values.ForEach(_ => _.TrySetResult(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject())));
                    if (clientObserver is not null)
                    {
                        await clientObserver.Unsubscribe();
                        clientObserver = null;
                    }
                }
            });

        CancellationTokenRegistration? register = null;

        register = context.CancellationToken.Register(() =>
        {
            logger.ObserverStreamDisconnected(observerId, connectionId);
            observableObserver?.OnCompleted();
            reducerMediator.Disconnected(observerId, connectionId, eventStoreName, namespaceName);
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
}
