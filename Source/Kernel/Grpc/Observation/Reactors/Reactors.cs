// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ObserverType = Cratis.Chronicle.Concepts.Observation.ObserverType;

namespace Cratis.Chronicle.Services.Observation.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="reactorMediator"><see cref="IReactorMediator"/> for observing actual events as they are made available.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for how often to check the subscription of a connected client.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class Reactors(
    IGrainFactory grainFactory,
    IReactorMediator reactorMediator,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<Reactors> activitySource,
    IOptions<ChronicleOptions> options,
    ILogger<Reactors> logger) : IReactors
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReactor>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingDeliveries = new PendingDeliveries<ObserverSubscriberResult>();
        IReactor clientObserver = null!;

        var messagesSubscription = messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterReactor register:
                    if (SystemStoreGuard.RegistrationRefusal(new(register.EventStore, register.Namespace, register.Reactor.ReactorId, register.Reactor.EventSequenceId, register.ConnectionId)) is { } refusal)
                    {
                        registrationTcs.TrySetException(refusal);
                        break;
                    }

                    logger.Registering(
                        register.Reactor.ReactorId,
                        register.EventStore,
                        register.Namespace,
                        register.Reactor.EventSequenceId,
                        register.ConnectionId);

                    registrationTcs.SetResult(register);
                    break;

                case ReactorResult result:
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

                    if (!pendingDeliveries.TryComplete(result.DeliveryId, result.Partition, subscriberResult))
                    {
                        logger.UnknownDeliveryResult(result.DeliveryId, result.Partition);
                    }

                    break;
            }
        });

        var connectionId = ConnectionId.NotSet;
        var observerId = ObserverId.Unspecified;
        var eventStoreName = EventStoreName.NotSet;
        var namespaceName = EventStoreNamespaceName.NotSet;
        IObserver<EventsToObserve>? observableObserver = null;
        (ReactorEventsObserver? deliver, ReactorReplayObserver? replay) = (null, null);
        var observable = Observable.Create<EventsToObserve>(async (observer, cancellationToken) =>
        {
            observableObserver = observer;

            try
            {
                var registration = await registrationTcs.Task;
                connectionId = registration.ConnectionId;
                observerId = registration.Reactor.ReactorId;
                eventStoreName = registration.EventStore;
                namespaceName = registration.Namespace;

                logger.Subscribing(
                    registration.Reactor.ReactorId,
                    registration.EventStore,
                    registration.Namespace,
                    registration.Reactor.EventSequenceId,
                    registration.ConnectionId);

                var key = new ConnectedObserverKey(
                    registration.Reactor.ReactorId,
                    registration.EventStore,
                    registration.Namespace,
                    registration.Reactor.EventSequenceId,
                    registration.ConnectionId);

                deliver = (partition, events, tcs, deliverWithin) =>
                {
                    var deliveryId = pendingDeliveries.Add(partition, tcs);
                    var eventsToObserve = events.Select(_ => _.ToContract(jsonSerializerOptions)).ToArray();
                    observer.OnNext(new EventsToObserve
                    {
                        Partition = partition.Value.ToString()!,
                        Events = eventsToObserve,
                        DeliveryId = deliveryId,
                        DeliverWithinMilliseconds = DeliveryDeadlines.ToMilliseconds(deliverWithin)
                    });
                };
                replay = (replayState, partition) => observer.OnNext(new EventsToObserve { Partition = partition, ReplayState = (Contracts.Observation.ReplayState)replayState });

                reactorMediator.Subscribe(
                    registration.Reactor.ReactorId,
                    registration.ConnectionId,
                    registration.EventStore,
                    registration.Namespace,
                    deliver);

                reactorMediator.SubscribeReplayNotifications(
                    registration.Reactor.ReactorId,
                    registration.ConnectionId,
                    registration.EventStore,
                    registration.Namespace,
                    replay);

                var definition = registration.Reactor.ToChronicle();

                // The parent is the long-lived Observe request, which the server deliberately does not trace.
                var requestActivity = Activity.Current;
                Activity.Current = null;
                try
                {
                    using var span = activitySource.Register();
                    span?.Activity?.Tag(key, ObserverType.Reactor);
                    clientObserver = grainFactory.GetGrain<IReactor>(key);
                    await clientObserver.SetDefinitionAndSubscribe(definition);
                }
                finally
                {
                    Activity.Current = requestActivity;
                }

                await ClientSubscriptionKeeper.KeepWhileConnected(
                    () => clientObserver.KeepSubscription(definition),
                    TimeSpan.FromSeconds(options.Value.ConnectedClients.ReviseIntervalSeconds),
                    registration.ConnectionId,
                    logger,
                    cancellationToken);

                logger.Disconnected(
                    registration.Reactor.ReactorId,
                    registration.EventStore,
                    registration.Namespace,
                    registration.Reactor.EventSequenceId,
                    registration.ConnectionId);
            }
            catch (OperationCanceledException)
            {
                logger.DisconnectedUngracefully(observerId, connectionId);
                observer.OnCompleted();
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
                pendingDeliveries.Disconnect(ObserverSubscriberResult.Disconnected());
                await ReleaseRegistration(clientObserver, observerId, connectionId, ownsRegistration);
                clientObserver = null!;
            }
        });

        CancellationTokenRegistration? register = null;

        register = context.CancellationToken.Register(() =>
        {
            logger.ObserverStreamDisconnected(observerId, connectionId);
            pendingDeliveries.Disconnect(ObserverSubscriberResult.Disconnected());
            observableObserver?.OnCompleted();
            DisconnectStream((observerId, connectionId, eventStoreName, namespaceName), deliver, replay);

            register?.Dispose();
        });

        return observable;
    }

    /// <inheritdoc/>
    public async Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default)
    {
        await SystemStoreGuard.Ensure(context, new(request.EventStore, request.ReactorId));
        var eventStore = storage.GetEventStore(request.EventStore);
        var exists = await eventStore.Reactors.Has(new(request.ReactorId));

        if (!exists)
        {
            return new HasReactorResponse { Exists = false, EventSequenceId = null };
        }

        var definition = await eventStore.Reactors.Get(new(request.ReactorId));
        return new HasReactorResponse { Exists = true, EventSequenceId = definition.EventSequenceId.Value };
    }

    bool DisconnectStream(
        (ObserverId ObserverId, ConnectionId ConnectionId, EventStoreName EventStore, EventStoreNamespaceName Namespace) key,
        ReactorEventsObserver? deliver,
        ReactorReplayObserver? replay) =>
        deliver is null || replay is null || reactorMediator.Disconnected(key.ObserverId, key.ConnectionId, key.EventStore, key.Namespace, deliver, replay);

    async Task ReleaseRegistration(IReactor? clientObserver, ObserverId observerId, ConnectionId connectionId, bool ownsRegistration)
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
}
