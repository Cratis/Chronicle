// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
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
    /// Subscribe to pages of events spanning partitions for a specific <see cref="ConnectionId"/>. A connection that does not subscribe
    /// to pages is given a page one partition at a time.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to subscribe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReactorPageObserver"/> delegate that will be called with pages.</param>
    void SubscribePages(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorPageObserver target);

    /// <summary>
    /// Notify that a page of events spanning partitions should be observed.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to send to.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to send to.</param>
    /// <param name="batches">The <see cref="ObserverPartitionBatch"/> of each partition in the page.</param>
    /// <param name="completions">The <see cref="TaskCompletionSource{T}"/> of each partition to return its <see cref="ObserverSubscriberResult"/> to.</param>
    /// <param name="deliverWithin">How long the kernel waits for the results, or null when it waits without a deadline.</param>
    /// <param name="origin">The <see cref="ObservationOrigin"/> the events came from.</param>
    void OnNextPage(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, IReadOnlyList<ObserverPartitionBatch> batches, IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>> completions, TimeSpan? deliverWithin = null, ObservationOrigin origin = ObservationOrigin.Unknown);

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
    /// <param name="deliverWithin">How long the kernel waits for the result, or null when it waits without a deadline.</param>
    /// <param name="origin">The <see cref="ObservationOrigin"/> the events came from.</param>
    void OnNext(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource, TimeSpan? deliverWithin = null, ObservationOrigin origin = ObservationOrigin.Unknown);

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
    /// Notify that a client stream has disconnected. The registrations are only removed when they are still
    /// the ones the disconnecting stream made; a stream that was replaced by a later one for the same key leaves
    /// its successor in place.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> for the client that disconnected.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> for the client that disconnected.</param>
    /// <param name="target"><see cref="ReactorEventsObserver"/> delegate the disconnecting stream registered.</param>
    /// <param name="replayTarget"><see cref="ReactorReplayObserver"/> delegate the disconnecting stream registered.</param>
    /// <returns>True if the disconnecting stream still owned the events registration, or none was registered; false if a later stream replaced it.</returns>
    bool Disconnected(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorEventsObserver target, ReactorReplayObserver replayTarget);
}
