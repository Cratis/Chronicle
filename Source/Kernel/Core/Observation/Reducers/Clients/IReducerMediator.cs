// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReducerMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReducerEventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerEventsObserver target);

    /// <summary>
    /// Subscribe to replay notifications for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReducerReplayObserver"/> delegate that will be called with replay notifications.</param>
    void SubscribeReplayNotifications(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerReplayObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to send to.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to send to.</param>
    /// <param name="operation"><see cref="ReduceOperation"/> to send.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReduceOperation operation, TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Subscribe to pages of partitions for a specific <see cref="ConnectionId"/>. A connection that does not subscribe
    /// to pages is given a page one partition at a time.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReducerPageObserver"/> delegate that will be called with pages.</param>
    /// <param name="pageDelivery">The <see cref="PageDeliveryDefinition"/> bounds the client declared, if any.</param>
    void SubscribePages(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerPageObserver target, PageDeliveryDefinition? pageDelivery = null);

    /// <summary>
    /// Gets the page bounds a connection declared when it subscribed to pages.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to get for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to get for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to get for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns>The <see cref="PageDeliveryDefinition"/>, or null when the connection does not receive pages.</returns>
    PageDeliveryDefinition? GetPageDelivery(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Notify that a page of partitions should be reduced.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to send to.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to send to.</param>
    /// <param name="operations">The <see cref="ReduceOperation"/> of each partition in the page.</param>
    /// <param name="completions">The <see cref="TaskCompletionSource{T}"/> of each partition to return its <see cref="ReducerSubscriberResult"/> to.</param>
    void OnNextPage(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, IReadOnlyList<ReduceOperation> operations, IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> completions);

    /// <summary>
    /// Notify that a replay has begun for a specific reducer.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> the replay began for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    void OnBeginReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Notify that a replay has ended for a specific reducer.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> the replay ended for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    void OnEndReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Notify that replay of a specific partition has begun for a specific reducer.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> the partition replay began for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    /// <param name="partition"><see cref="Key"/> for the partition being replayed.</param>
    void OnBeginReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition);

    /// <summary>
    /// Notify that replay of a specific partition has ended for a specific reducer.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> the partition replay ended for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the replay is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the replay is for.</param>
    /// <param name="partition"><see cref="Key"/> for the partition that was replayed.</param>
    void OnEndReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition);

    /// <summary>
    /// Notify that a client stream has disconnected. The registrations are only removed when they are still
    /// the ones the disconnecting stream made; a stream that was replaced by a later one for the same key leaves
    /// its successor in place.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> for the client that disconnected.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> for the client that disconnected.</param>
    /// <param name="target"><see cref="ReducerEventsObserver"/> delegate the disconnecting stream registered.</param>
    /// <param name="replayTarget"><see cref="ReducerReplayObserver"/> delegate the disconnecting stream registered.</param>
    /// <returns>True if the disconnecting stream still owned the events registration, or none was registered; false if a later stream replaced it.</returns>
    bool Disconnected(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerEventsObserver target, ReducerReplayObserver replayTarget);
}
