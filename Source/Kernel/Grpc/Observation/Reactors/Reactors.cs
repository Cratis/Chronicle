// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Clients;
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
using Grpc.Core;
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
/// <param name="ownership">Local connection ownership.</param>
/// <param name="owners">The caller's principal resolver.</param>
internal sealed class Reactors(
    IGrainFactory grainFactory,
    IReactorMediator reactorMediator,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<Reactors> activitySource,
    IOptions<ChronicleOptions> options,
    ILogger<Reactors> logger,
    IConnectionOwnership ownership,
    IConnectionOwnerResolver owners) : IReactors
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default)
    {
        var owner = owners.Resolve(context.ServerCallContext?.GetHttpContext().User, context.ServerCallContext is not null);
        return Observable.Defer(() => ObserveOwned(messages, context, owner));
    }

    IObservable<EventsToObserve> ObserveOwned(IObservable<ReactorMessage> messages, CallContext context, ConnectionOwner owner)
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
                    logger.Registering(
                        register.Reactor.ReactorId,
                        register.EventStore,
                        register.Namespace,
                        register.Reactor.EventSequenceId,
                        register.ConnectionId);

                    registrationTcs.TrySetResult(register);
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

        ConnectionClaim? claim = null;
        var connectionId = ConnectionId.NotSet;
        var observerId = ObserverId.Unspecified;
        var eventStoreName = EventStoreName.NotSet;
        var namespaceName = EventStoreNamespaceName.NotSet;
        object? streamToken = null;
        ReactorMediatorKey? subscriptionKey = null;
        bool IsCurrentStream() => streamToken is not null &&
            ReferenceEquals(ownership.GetClaim(connectionId), claim) &&
            ownership.IsCurrentSubscription(connectionId, subscriptionKey!, streamToken);

        return Observable.Create<EventsToObserve>(async (observer, cancellationToken) =>
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationToken);
            Exception? failure = null;
            try
            {
                var registration = await registrationTcs.Task.WaitAsync(lifetime.Token);
                Concepts.Observation.Reactors.ReactorDefinition definition;
                using (await ownership.Enter(registration.ConnectionId, lifetime.Token))
                {
                    lifetime.Token.ThrowIfCancellationRequested();
                    claim = ownership.GetClaim(registration.ConnectionId);
                    if (ownership.Authorize(registration.ConnectionId, owner) != ConnectionAccess.Allowed)
                    {
                        logger.ObservationRejected(registration.ConnectionId);
                        throw new RpcException(new Status(StatusCode.PermissionDenied, "The connection belongs to another caller."));
                    }

                    definition = registration.Reactor.ToChronicle();
                    connectionId = registration.ConnectionId;
                    observerId = registration.Reactor.ReactorId;
                    eventStoreName = registration.EventStore;
                    namespaceName = registration.Namespace;
                    subscriptionKey = new(registration.Reactor.ReactorId, connectionId, eventStoreName, namespaceName);
                    streamToken = ownership.Subscribe(connectionId, subscriptionKey);

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

                    reactorMediator.Subscribe(
                        registration.Reactor.ReactorId,
                        registration.ConnectionId,
                        registration.EventStore,
                        registration.Namespace,
                        (partition, events, tcs, deliverWithin) =>
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
                        });

                    reactorMediator.SubscribeReplayNotifications(
                        registration.Reactor.ReactorId,
                        registration.ConnectionId,
                        registration.EventStore,
                        registration.Namespace,
                        (replayState, partition) => observer.OnNext(new EventsToObserve { Partition = partition, ReplayState = (Contracts.Observation.ReplayState)replayState }));

                    using (var span = activitySource.Register())
                    {
                        span?.Activity?.Tag(key, ObserverType.Reactor);
                        clientObserver = grainFactory.GetGrain<IReactor>(key);
                        await clientObserver.SetDefinitionAndSubscribe(definition);
                    }
                }

                await ClientSubscriptionKeeper.KeepWhileConnected(
                    async () =>
                    {
                        using var gate = await ownership.Enter(connectionId, lifetime.Token);
                        lifetime.Token.ThrowIfCancellationRequested();
                        if (!IsCurrentStream())
                        {
                            lifetime.Cancel();
                            return;
                        }

                        await clientObserver.KeepSubscription(definition);
                    },
                    TimeSpan.FromSeconds(options.Value.ConnectedClients.ReviseIntervalSeconds),
                    registration.ConnectionId,
                    logger,
                    lifetime.Token);

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
            }
            catch (Exception ex)
            {
                logger.Disengage(observerId, connectionId, ex);
                context.RecordFailure(ex);
                failure = ex;
            }
            finally
            {
                messagesSubscription.Dispose();
                pendingDeliveries.Disconnect(ObserverSubscriberResult.Disconnected());
                if (streamToken is not null)
                {
                    using var gate = await ownership.Enter(connectionId);
                    try
                    {
                        if (IsCurrentStream())
                        {
                            reactorMediator.Disconnected(observerId, connectionId, eventStoreName, namespaceName);
                            if (clientObserver is not null) await clientObserver.Unsubscribe();
                        }
                    }
                    finally
                    {
                        ownership.ReleaseSubscription(connectionId, subscriptionKey!, streamToken);
                    }
                }
            }

            if (failure is not null) observer.OnError(failure);
            else observer.OnCompleted();
        });
    }

    /// <inheritdoc/>
    public async Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        var exists = await eventStore.Reactors.Has(new(request.ReactorId));

        if (!exists)
        {
            return new HasReactorResponse { Exists = false, EventSequenceId = null };
        }

        var definition = await eventStore.Reactors.Get(new(request.ReactorId));
        return new HasReactorResponse { Exists = true, EventSequenceId = definition.EventSequenceId.Value };
    }
}
