// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Json;
using Cratis.Collections;
using Microsoft.Extensions.Logging;
using NJsonSchema;
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
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class Reducers(
    IGrainFactory grainFactory,
    IReducerMediator reducerMediator,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<Reducers> logger) : IReducers
{
    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentDictionary<EventSourceId, TaskCompletionSource<ReducerSubscriberResult>> reducerResultTcs = [];
        IReducer? clientObserver = null;

        var model = new Model(ModelName.NotSet, new JsonSchema());

        messages.Subscribe(message =>
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

                    registrationTcs.SetResult(register);
                    break;

                case ReducerResult result:
                    var state = result.State switch
                    {
                        ObservationState.None => ObserverSubscriberState.None,
                        ObservationState.Success => ObserverSubscriberState.Ok,
                        ObservationState.Failed => ObserverSubscriberState.Failed,
                        _ => ObserverSubscriberState.None
                    };

                    var modelAsJson = (result.ModelState is null) ? [] : JsonNode.Parse(result.ModelState)!.AsObject();

                    var subscriberResult = new ReducerSubscriberResult(
                        new ObserverSubscriberResult(
                            state,
                            result.LastSuccessfulObservation,
                            result.ExceptionMessages,
                            result.ExceptionStackTrace),
                        expandoObjectConverter.ToExpandoObject(modelAsJson, model.Schema));

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
                        (reduceOperation, tcs) =>
                        {
                            reducerResultTcs[reduceOperation.Partition] = tcs;
                            var initialState = reduceOperation.InitialState is null ? null : expandoObjectConverter.ToJsonObject(reduceOperation.InitialState, model.Schema).ToString();
                            var message = new ReduceOperationMessage()
                            {
                                Partition = reduceOperation.Partition.Value.ToString()!,
                                InitialState = initialState,
                                Events = reduceOperation.Events.Select(_ => _.ToContract()).ToArray()
                            };

                            observer.OnNext(message);
                        });

                    using (Tracing.RegisterObserver(key, ObserverType.Reducer))
                    {
                        var reducerDefinition = registration.Reducer.ToChronicle();

                        clientObserver = grainFactory.GetGrain<IReducer>(key);
                        await clientObserver.SetDefinitionAndSubscribe(reducerDefinition);

                        var modelSchema = await JsonSchema.FromJsonAsync(reducerDefinition.Model.Schema);
                        model = new Model(reducerDefinition.Model.Name, modelSchema);
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
                    reducerMediator.Disconnected(observerId, connectionId);
                    reducerResultTcs.Values.ForEach(_ => _.SetResult(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject())));
                    await clientObserver!.Unsubscribe();
                    clientObserver = null;
                }
            });

        CancellationTokenRegistration? register = null;

        register = context.CancellationToken.Register(() =>
        {
            logger.ObserverStreamDisconnected(observerId, connectionId);
            observableObserver?.OnCompleted();

            clientObserver?.Unsubscribe().GetAwaiter().GetResult();

            reducerMediator.Disconnected(observerId, connectionId);
            reducerResultTcs.Values.ForEach(_ => _.SetResult(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject())));
            clientObserver = null;

            register?.Dispose();
        });

        return observable;
    }
}
