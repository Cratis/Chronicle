// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;
#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents the state of an observer.
/// </summary>
public class ObserverState
{
    /// <summary>
    /// Gets or sets the unique identifier of the observer.
    /// </summary>
    public ObserverId Id { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the last handled event sequence number.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the next event sequence number.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the current running state of the observer.
    /// </summary>
    public ObserverRunningState RunningState { get; set; } = ObserverRunningState.Unknown;

    /// <summary>
    /// Gets or sets the set of partitions being replayed.
    /// </summary>
    public IEnumerable<Key> ReplayingPartitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the set of partitions being caught up.
    /// </summary>
    public IEnumerable<Key> CatchingUpPartitions { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the observer is currently replaying events.
    /// </summary>
    public bool IsReplaying { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the observer subscribes to all event types.
    /// </summary>
    public bool SubscribesToAllEvents { get; set; }
}
