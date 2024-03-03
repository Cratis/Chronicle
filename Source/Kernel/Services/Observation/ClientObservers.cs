// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Connections;
using Cratis.Events;
using Cratis.Kernel.Contracts.Observation;
using Cratis.Kernel.Grains.Observation;
using Cratis.Kernel.Grains.Observation.Clients;
using Cratis.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Kernel.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IClientObservers"/>.
/// </summary>
public class ClientObservers : IClientObservers
{
    readonly IGrainFactory _grainFactory;
    readonly IObserverMediator _observerMediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="observerMediator"><see cref="IObserverMediator"/> for observing actual events as they are made available.</param>
    public ClientObservers(
        IGrainFactory grainFactory,
        IObserverMediator observerMediator)
    {
        _grainFactory = grainFactory;
        _observerMediator = observerMediator;
    }

    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ObserverClientMessage> messages, CallContext context = default)
    {
        var registrationTcs = new TaskCompletionSource<RegisterObserver>();
        TaskCompletionSource<ObserverSubscriberResult>? observationResultTcs = null;
        TaskCompletionSource<IEnumerable<AppendedEvent>>? eventsTcs;

        IClientObserver clientObserver = null!;

        messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterObserver register:
                    var key = new ConnectedObserverKey(
                        register.EventStoreName,
                        register.Namespace,
                        register.EventSequenceId,
                        register.ConnectionId);
                    clientObserver = _grainFactory.GetGrain<IClientObserver>(Guid.Parse(register.ObserverId), keyExtension: key);
                    clientObserver.Start(register.ObserverName, register.EventTypes.Select(_ => _.ToKernel()).ToArray());

                    registrationTcs.SetResult(register);
                    break;

                case ObservationResult result:
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
                _observerMediator.Subscribe(
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
                _observerMediator.Disconnected(observerId, connectionId);
                observationResultTcs?.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                _observerMediator.Disconnected(observerId, connectionId);
                observationResultTcs?.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
                observer.OnError(ex);
            }
        });
    }
}
