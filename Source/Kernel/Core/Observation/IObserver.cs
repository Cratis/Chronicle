// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.StateMachines;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines an observer in the system.
/// </summary>
public interface IObserver : IStateMachine<ObserverState>, IGrainWithStringKey
{
    /// <summary>
    /// Ensure the observer existence.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Get the definition from the observer.
    /// </summary>
    /// <returns>The <see cref="ObserverDefinition"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverDefinition> GetDefinition();

    /// <summary>
    /// Get the state from the observer.
    /// </summary>
    /// <returns>The <see cref="ObserverState"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverState> GetState();

    /// <summary>
    /// Set the handled stats for the observer.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number.</param>
    /// <returns>Awaitable task.</returns>
    Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Get the subscription for the observer.
    /// </summary>
    /// <returns>Tbe <see cref="ObserverSubscription"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverSubscription> GetSubscription();

    /// <summary>
    /// Check if the observer has a subscription subscribed.
    /// </summary>
    /// <returns>True if it has, false if not.</returns>
    [AlwaysInterleave]
    Task<bool> IsSubscribed();

    /// <summary>
    /// Get the event types that the observer is observing.
    /// </summary>
    /// <returns>Collection of <see cref="EventType"/>.</returns>
    [AlwaysInterleave]
    Task<IEnumerable<EventType>> GetEventTypes();

    /// <summary>
    /// Subscribe to observer.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">Type of <see cref="IObserverSubscriber"/> to subscribe.</typeparam>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
    /// <param name="siloAddress"><see cref="SiloAddress"/> the subscriber is connected to.</param>
    /// <param name="subscriberArgs">Optional arguments associated with the subscription.</param>
    /// <param name="isReplayable">Whether the observer supports replay scenarios. Defaults to true.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = default,
        bool isReplayable = true)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Unsubscribe from the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Rewind the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Replay();

    /// <summary>
    /// Rewind the observer for a specific partition.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartition(Key partition);

    /// <summary>
    /// Rewind the observer for a specific partition to a specific sequence number.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to rewind to.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Notify that the observer has been replayed.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> it has been replayed to.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition has been replayed.
    /// </summary>
    /// <param name="partition">The partition that has been replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that as handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailed(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace);

    /// <summary>
    /// Notify that the partition has recovered.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that as handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition has partially recovered.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that as handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Catch up the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task CatchUp();

    /// <summary>
    /// Register partitions that the observer is catching up.
    /// </summary>
    /// <param name="partitions">Collection of <see cref="Key">partitions</see>.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task RegisterCatchingUpPartitions(IEnumerable<Key> partitions);

    /// <summary>
    /// Notify that the observer has been caught up.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that as handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task CaughtUp(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition was caught.
    /// </summary>
    /// <param name="partition">The partition that has caught up.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that as handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Attempt to recover a failed partition.
    /// </summary>
    /// <param name="partition">The partition that is failed.</param>
    /// <returns>Awaitable task.</returns>
    Task TryStartRecoverJobForFailedPartition(Key partition);

    /// <summary>
    /// Attempt to recover all failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task TryRecoverAllFailedPartitions();

    /// <summary>
    /// Handle events for an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to handle events for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(Key partition, IEnumerable<AppendedEvent> events);
}
