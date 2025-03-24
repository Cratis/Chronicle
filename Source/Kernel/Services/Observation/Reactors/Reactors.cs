// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Reactors.Clients;
using Cratis.Collections;
using Microsoft.Extensions.Logging;
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
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class Reactors(
    IGrainFactory grainFactory,
    IReactorMediator reactorMediator,
    ILogger<Reactors> logger) : IReactors
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default)
    {
        logger.Observe();

        var registrationTcs = new TaskCompletionSource<RegisterReactor>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentDictionary<EventSourceId, TaskCompletionSource<ObserverSubscriberResult>> reactorResultTcs = [];
        IReactor clientObserver = null!;

        messages.Subscribe(message =>
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

                    if (reactorResultTcs.TryGetValue(result.Partition, out var tcs))
                    {
                        tcs.SetResult(subscriberResult);
                        reactorResultTcs.TryRemove(result.Partition, out _);
                    }

                    break;
            }
        });

        var connectionId = ConnectionId.NotSet;
        var observerId = ObserverId.Unspecified;
        IObserver<EventsToObserve>? observableObserver = null;
        var observable = Observable.Create<EventsToObserve>(async (observer, cancellationToken) =>
        {
            observableObserver = observer;

            try
            {
                var registration = await registrationTcs.Task;
                connectionId = registration.ConnectionId;
                observerId = registration.Reactor.ReactorId;

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
                    (partition, events, tcs) =>
                    {
                        reactorResultTcs[partition] = tcs;
                        var eventsToObserve = events.Select(_ => _.ToContract()).ToArray();
                        observer.OnNext(new EventsToObserve { Partition = partition.Value.ToString()!, Events = eventsToObserve });
                    });

                using (Tracing.RegisterObserver(key, ObserverType.Reactor))
                {
                    clientObserver = grainFactory.GetGrain<IReactor>(key);
                    await clientObserver.SetDefinitionAndSubscribe(registration.Reactor.ToChronicle());
                }

                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

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
                observer.OnError(ex);
            }
            finally
            {
                reactorMediator.Disconnected(observerId, connectionId);
                reactorResultTcs.Values.ForEach(_ => _.SetResult(ObserverSubscriberResult.Disconnected()));
                await clientObserver!.Unsubscribe();
                clientObserver = null!;
            }
        });

        CancellationTokenRegistration? register = null;

        register = context.CancellationToken.Register(() =>
        {
            logger.ObserverStreamDisconnected(observerId, connectionId);
            observableObserver?.OnCompleted();
            clientObserver?.Unsubscribe().GetAwaiter().GetResult();
            reactorMediator.Disconnected(observerId, connectionId);
            reactorResultTcs.Values.ForEach(_ => _.TrySetCanceled());
            register?.Dispose();
        });

        return observable;
    }
}
