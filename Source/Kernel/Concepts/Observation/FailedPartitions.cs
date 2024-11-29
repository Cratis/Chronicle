// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the state of all failed partitions within an observer.
/// </summary>
public class FailedPartitions
{
    readonly List<FailedPartition> _resolvedPartitions = [];
    readonly Dictionary<Key, FailedPartition> _partitions = [];

    /// <summary>
    /// Gets or sets the failed partitions for the observer.
    /// </summary>
    public IEnumerable<FailedPartition> Partitions
    {
        get => _partitions.Values;
        set
        {
            _partitions = failedPartitions.ToDictionary(_ => _.Partition, _ => _);
        }
    }

    /// <summary>
    /// Gets the resolved partitions for the observer.
    /// </summary>
    public IEnumerable<FailedPartition> ResolvedPartitions => _resolvedPartitions;

    /// <summary>
    /// Gets a value indicating whether there are any failed partitions.
    /// </summary>
    public bool HasFailedPartitions => _partitions.Count > 0;

    /// <summary>
    /// Check whether a partition is failed.
    /// </summary>
    /// <param name="partition">Partition to check.</param>
    /// <returns>True if failed, false if not.</returns>
    public bool IsFailed(Key partition) => _partitions.ContainsKey(partition);

    /// <summary>
    /// Try to get a failed partition by its partition identifier.
    /// </summary>
    /// <param name="partition">Partition to get.</param>
    /// <param name="failedPartition">The optional failed partition.</param>
    /// <returns>True when failed partition exists, false if not.</returns>
    public bool TryGet(Key partition, [NotNullWhen(true)]out FailedPartition? failedPartition) => _partitions.TryGetValue(partition, out failedPartition);

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
        if (!TryGet(partition, out var failure))
        {
            failure = new()
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
    public void Remove(Key partition)
    {
        if (!TryGet(partition, out var failedPartition)) return;

        _resolvedPartitions.Add(failedPartition);
        _partitions.Remove(partition);
    }

    void Add(FailedPartition failedPartition) => _partitions.Add(failedPartition.Partition, failedPartition);
}
