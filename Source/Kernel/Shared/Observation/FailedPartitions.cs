// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;

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
    public bool IsFailed(Key partition) => _partitions.Exists(_ => _.Partition.Equals(partition));

    /// <summary>
    /// Gets a failed partition by its partition identifier.
    /// </summary>
    /// <param name="partition">Partition to get.</param>
    /// <returns>The failed partition.</returns>
    public FailedPartition? Get(Key partition) => _partitions.Find(_ => _.Partition == partition);

    /// <summary>
    /// Register an attempt for a partition.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to register for.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> the attempt was for.</param>
    /// <param name="messages">Collection of messages associated with the error.</param>
    /// <param name="stackTrace">The stack trace associated with the error.</param>
    /// <returns>A <see cref="FailedPartition"/> instance.</returns>
    public FailedPartition RegisterAttempt(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> messages,
        string stackTrace)
    {
        FailedPartition failure;
        if (IsFailed(partition))
        {
            failure = Get(partition)!;
        }
        else
        {
            failure = new FailedPartition
            {
                Id = FailedPartitionId.New(),
                Partition = partition
            };

            Add(failure);
        }

        failure.AddAttempt(new()
        {
            Occurred = DateTimeOffset.UtcNow,
            SequenceNumber = sequenceNumber,
            Messages = messages,
            StackTrace = stackTrace
        });

        return failure;
    }

    /// <summary>
    /// Remove a failed partition.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to remove.</param>
    public void Remove(Key partition) => _partitions.RemoveAll(_ => _.Partition == partition);
}
