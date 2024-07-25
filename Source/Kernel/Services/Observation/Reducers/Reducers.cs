// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Observation;
using NJsonSchema;
using ProtoBuf.Grpc;

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
public class Reducers(
    IGrainFactory grainFactory,
    IReducerMediator reducerMediator,
    IExpandoObjectConverter expandoObjectConverter) : IReducers
{
    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        var registrationTcs = new TaskCompletionSource<RegisterReducer>();
        TaskCompletionSource<ReducerSubscriberResult>? reducerResultTcs = null;
        TaskCompletionSource<ReduceOperation>? reduceContextTcs;

        IReducer clientObserver = null!;

        var model = new Model(ModelName.NotSet, new JsonSchema());

        messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterReducer register:
                    var key = new ConnectedObserverKey(
                        register.Reducer.ReducerId,
                        register.EventStoreName,
                        register.Namespace,
                        register.Reducer.EventSequenceId,
                        register.ConnectionId);
                    clientObserver = grainFactory.GetGrain<IReducer>(key);
                    var reducerDefinition = register.Reducer.ToChronicle();
                    clientObserver.SetDefinitionAndSubscribe(reducerDefinition);

                    var modelSchema = JsonSchema.FromJsonAsync(reducerDefinition.Model.Schema).GetAwaiter().GetResult();
                    model = new Model(reducerDefinition.Model.Name, modelSchema);

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

                    reducerResultTcs?.SetResult(subscriberResult);
                    break;
            }
        });

        return Observable.Create<ReduceOperationMessage>(async (observer, cancellationToken) =>
        {
            var connectionId = ConnectionId.NotSet;
            var observerId = ObserverId.Unspecified;

            try
            {
                var registration = await registrationTcs.Task;
                connectionId = registration.ConnectionId;
                observerId = registration.Reducer.ReducerId;

                reduceContextTcs = new TaskCompletionSource<ReduceOperation>();
                reducerMediator.Subscribe(
                    registration.Reducer.ReducerId,
                    registration.ConnectionId,
                    (events, tcs) =>
                    {
                        reducerResultTcs = tcs;
                        reduceContextTcs.SetResult(events);
                    });

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var operationContext = await reduceContextTcs.Task;
                    var message = new ReduceOperationMessage()
                    {
                        InitialState = expandoObjectConverter.ToJsonObject(operationContext.InitialState, model.Schema).ToString(),
                        Events = operationContext.Events.Select(_ => _.ToContract()).ToArray()
                    };

                    observer.OnNext(message);
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    reduceContextTcs = new TaskCompletionSource<ReduceOperation>();
                }
            }
            catch (OperationCanceledException)
            {
                reducerMediator.Disconnected(observerId, connectionId);
                reducerResultTcs?.SetResult(new(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable), new ExpandoObject()));
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                reducerMediator.Disconnected(observerId, connectionId);
                reducerResultTcs?.SetResult(new(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable), new ExpandoObject()));
                observer.OnError(ex);
            }
        });
    }
}
