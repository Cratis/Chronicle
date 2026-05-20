// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
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
    /// Notify that a client has disconnected.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> for the client that disconnected.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> for the client that disconnected.</param>
    void Disconnected(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
