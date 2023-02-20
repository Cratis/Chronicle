// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the state used for an observer.
/// </summary>
public class ObserverState
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "observer-state";

    /// <summary>
    /// The name of the storage provider used for working with this type of state during catch-up.
    /// </summary>
    public const string CatchUpStorageProvider = "observer-state-catchup";

    /// <summary>
    /// The name of the storage provider used for working with this type of state during replay.
    /// </summary>
    public const string ReplayStorageProvider = "observer-state-replay";

    /// <summary>
    /// Gets or sets the identifier of the observer state.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types the observer is observing.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the state is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the state is for.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets a friendly name for the observer.
    /// </summary>
    public ObserverName Name { get; set; } = ObserverName.NotSpecified;

    /// <summary>
    /// Gets or sets the <see cref="ObserverType"/>.
    /// </summary>
    public ObserverType Type { get; set; } = ObserverType.Unknown;

    /// <summary>
    /// Gets or sets the expected next event sequence number into the event log.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the last handled event in the event log, ever. This value will never reset during a rewind.
    /// </summary>
    public EventSequenceNumber LastHandled { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the running state.
    /// </summary>
    public ObserverRunningState RunningState { get; set; } = ObserverRunningState.New;

    /// <summary>
    /// Gets or sets the failed partitions for the observer.
    /// </summary>
    public IEnumerable<FailedPartition> FailedPartitions
    {
        get => _failedPartitions;
        set => _failedPartitions = new(value);
    }

    /// <summary>
    /// Gets or sets the failed partitions for the observer.
    /// </summary>
    public IEnumerable<RecoveringFailedObserverPartition> RecoveringPartitions
    {
        get => _partitionsBeingRecovered;
        set => _partitionsBeingRecovered = new(value);
    }

    /// <summary>
    /// Gets whether or not there are any failed partitions.
    /// </summary>
    public bool HasFailedPartitions => _failedPartitions.Count > 0;

    /// <summary>
    /// Gets whether or not there are any partitions being recovered.
    /// </summary>
    public bool IsRecoveringAnyPartition => _partitionsBeingRecovered.Count > 0;

    /// <summary>
    /// Gets whether or not the observer is in disconnected state. Meaning that there is no subscriber to it.
    /// </summary>
    public bool IsDisconnected => RunningState == ObserverRunningState.Disconnected;

    List<FailedPartition> _failedPartitions = new();
    List<RecoveringFailedObserverPartition> _partitionsBeingRecovered = new();

    /// <summary>
    /// Add a failed partition.
    /// </summary>
    /// <param name="failedPartition"><see cref="FailedPartition"/> to add.</param>
    public void AddFailedPartition(FailedPartition failedPartition) => _failedPartitions.Add(failedPartition);

    /// <summary>
    /// Check whether or not a partition is failed.
    /// </summary>
    /// <param name="partition">Partition to check.</param>
    /// <returns>True if failed, false if not.</returns>
    public bool IsPartitionFailed(EventSourceId partition) => _failedPartitions.Any(_ => _.Partition == partition);

    /// <summary>
    /// Gets a failed partition by its partition identifier.
    /// </summary>
    /// <param name="partition">Partition to get.</param>
    /// <returns>The failed partition.</returns>
    public FailedPartition GetFailedPartition(EventSourceId partition) => _failedPartitions.Find(_ => _.Partition == partition);
}
