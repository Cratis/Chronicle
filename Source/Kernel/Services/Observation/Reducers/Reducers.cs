// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Clients;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="Contracts.Observation.Reducers.IReducers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="observerMediator"><see cref="IObserverMediator"/> for observing actual events as they are made available.</param>
public class Reducers(
    IGrainFactory grainFactory,
    IObserverMediator observerMediator) : Contracts.Observation.Reducers.IReducers
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        var registrationTcs = new TaskCompletionSource<RegisterReducer>();
        TaskCompletionSource<ObserverSubscriberResult>? observationResultTcs = null;
        TaskCompletionSource<IEnumerable<AppendedEvent>>? eventsTcs;

        IReducer clientObserver = null!;

        messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterReducer register:
                    var key = new ConnectedObserverKey(
                        register.ReducerId,
                        register.EventStoreName,
                        register.Namespace,
                        register.EventSequenceId,
                        register.ConnectionId);
                    clientObserver = grainFactory.GetGrain<IReducer>(key);
                    clientObserver.Start(register.EventTypes.Select(_ => _.ToChronicle()).ToArray());

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
                    var subscriberResult = new ObserverSubscriberResult(
                        state,
                        result.LastSuccessfulObservation,
                        result.ExceptionMessages,
                        result.ExceptionStackTrace);

                    observationResultTcs?.SetResult(subscriberResult);
                    break;
            }
        });

        return Observable.Create<EventsToObserve>(async (observer, cancellationToken) =>
        {
            var connectionId = ConnectionId.NotSet;
            var observerId = ObserverId.Unspecified;

            try
            {
                var registration = await registrationTcs.Task;
                connectionId = registration.ConnectionId;
                observerId = registration.ReducerId;

                eventsTcs = new TaskCompletionSource<IEnumerable<AppendedEvent>>();
                observerMediator.Subscribe(
                    registration.ReducerId,
                    registration.ConnectionId,
                    (events, tcs) =>
                    {
                        observationResultTcs = tcs;
                        eventsTcs.SetResult(events);
                    });

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var events = await eventsTcs.Task;
                    var eventsToObserve = events.Select(_ => _.ToContract()).ToArray();
                    observer.OnNext(new EventsToObserve { Events = eventsToObserve });
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    eventsTcs = new TaskCompletionSource<IEnumerable<AppendedEvent>>();
                }
            }
            catch (OperationCanceledException)
            {
                observerMediator.Disconnected(observerId, connectionId);
                observationResultTcs?.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observerMediator.Disconnected(observerId, connectionId);
                observationResultTcs?.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
                observer.OnError(ex);
            }
        });
    }
}
