// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReactorMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReactorEventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorEventsObserver target);

    /// <summary>
    /// Subscribe to replay notifications for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReactorReplayObserver"/> delegate that will be called with replay notifications.</param>
    void SubscribeReplayNotifications(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorReplayObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to send to.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to send to.</param>
    /// <param name="partition"><see cref="Key"/> for the partition.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Notify that a replay has begun for a specific reactor.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> the replay began for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    void OnBeginReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Notify that a replay has ended for a specific reactor.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> the replay ended for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    void OnEndReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Notify that replay of a specific partition has begun for a specific reactor.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> the partition replay began for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    /// <param name="partition"><see cref="Key"/> for the partition being replayed.</param>
    void OnBeginReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition);

    /// <summary>
    /// Notify that replay of a specific partition has ended for a specific reactor.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> the partition replay ended for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    /// <param name="partition"><see cref="Key"/> for the partition that was replayed.</param>
    void OnEndReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition);

    /// <summary>
    /// Notify that a client has disconnected.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> for the client that disconnected.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> for the client that disconnected.</param>
    void Disconnected(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
