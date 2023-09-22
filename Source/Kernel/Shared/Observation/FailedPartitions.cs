// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Represents the state of all failed partitions within an observer.
/// </summary>
public class FailedPartitions
{
    List<FailedPartition> _partitions = new();

    /// <summary>
    /// Gets or sets the failed partitions for the observer.
    /// </summary>
    public IEnumerable<FailedPartition> Partitions
    {
        get => _partitions;
        set => _partitions = new(value);
    }

    /// <summary>
    /// Gets whether or not there are any failed partitions.
    /// </summary>
    public bool HasFailedPartitions => _partitions.Count > 0;

    /// <summary>
    /// Add a failed partition.
    /// </summary>
    /// <param name="failedPartition"><see cref="FailedPartition"/> to add.</param>
    public void Add(FailedPartition failedPartition) => _partitions.Add(failedPartition);

    /// <summary>
    /// Check whether or not a partition is failed.
    /// </summary>
    /// <param name="partition">Partition to check.</param>
    /// <returns>True if failed, false if not.</returns>
    public bool IsFailed(EventSourceId partition) => _partitions.Exists(_ => _.Partition == partition);

    /// <summary>
    /// Gets a failed partition by its partition identifier.
    /// </summary>
    /// <param name="partition">Partition to get.</param>
    /// <returns>The failed partition.</returns>
    public FailedPartition? Get(EventSourceId partition) => _partitions.Find(_ => _.Partition == partition);
}
