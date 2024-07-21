// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Reactions.Clients;
using Cratis.Chronicle.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactions;

/// <summary>
/// Represents an implementation of <see cref="IReactions"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="observerMediator"><see cref="IReactionMediator"/> for observing actual events as they are made available.</param>
public class Reactions(
    IGrainFactory grainFactory,
    IReactionMediator observerMediator) : IReactions
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReactionMessage> messages, CallContext context = default)
    {
        var registrationTcs = new TaskCompletionSource<RegisterReaction>();
        TaskCompletionSource<ObserverSubscriberResult>? observationResultTcs = null;
        TaskCompletionSource<IEnumerable<AppendedEvent>>? eventsTcs;

        IReaction clientObserver = null!;

        messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterReaction register:
                    var key = new ConnectedObserverKey(
                        register.ObserverId,
                        register.EventStoreName,
                        register.Namespace,
                        register.EventSequenceId,
                        register.ConnectionId);
                    clientObserver = grainFactory.GetGrain<IReaction>(key);
                    clientObserver.Start(register.EventTypes.Select(_ => _.ToChronicle()).ToArray());

                    registrationTcs.SetResult(register);
                    break;

                case ReactionResult result:
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
                observerId = registration.ObserverId;

                eventsTcs = new TaskCompletionSource<IEnumerable<AppendedEvent>>();
                observerMediator.Subscribe(
                    registration.ObserverId,
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
