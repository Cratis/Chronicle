// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the state of failed partitions. Typically holds the failures of a single observer, but the
/// cross-observer storage queries (all failed partitions of a namespace, or of a set of observers) return
/// their combined result through this type as well — so the same partition key may legitimately appear
/// once per observer, and entries are identified by their own identity rather than their partition.
/// </summary>
public class FailedPartitions
{
    /// <summary>
    /// Maximum number of resolved partitions to keep in memory for observability.
    /// Oldest entries are evicted once this limit is reached to prevent unbounded memory growth.
    /// </summary>
    const int MaxResolvedPartitions = 100;

    readonly List<FailedPartition> _resolvedPartitions = [];
    Dictionary<FailedPartitionId, FailedPartition> _partitions = [];

    /// <summary>
    /// Gets or sets the failed partitions.
    /// </summary>
    public IEnumerable<FailedPartition> Partitions
    {
        get => _partitions.Values;
        set
        {
            // Keyed by the entry identity rather than the partition: a partition is only unique within
            // one observer, and the cross-observer queries feed this setter with several observers'
            // failures at once. The identity is also immutable, unlike the observer — which the grain
            // storage provider stamps onto an entry while it already sits in this dictionary. Built with
            // the indexer rather than ToDictionary so reporting stored data can never throw on it.
            var partitions = new Dictionary<FailedPartitionId, FailedPartition>();
            foreach (var partition in value)
            {
                partitions[partition.Id] = partition;
            }

            _partitions = partitions;
        }
    }

    /// <summary>
    /// Gets the resolved partitions.
    /// </summary>
    public IEnumerable<FailedPartition> ResolvedPartitions => _resolvedPartitions;

    /// <summary>
    /// Gets a value indicating whether there are any failed partitions.
    /// </summary>
    public bool HasFailedPartitions => _partitions.Count > 0;

    /// <summary>
    /// Check whether a partition is failed, for any observer represented in this instance.
    /// </summary>
    /// <param name="partition">Partition to check.</param>
    /// <returns>True if failed, false if not.</returns>
    public bool IsFailed(Key partition) => TryGet(partition, out _);

    /// <summary>
    /// Try to get a failed partition by its partition identifier. When this instance holds the failures
    /// of several observers, the first entry for the partition is returned.
    /// </summary>
    /// <param name="partition">Partition to get.</param>
    /// <param name="failedPartition">The optional failed partition.</param>
    /// <returns>True when failed partition exists, false if not.</returns>
    public bool TryGet(Key partition, [NotNullWhen(true)] out FailedPartition? failedPartition)
    {
        failedPartition = _partitions.Values.FirstOrDefault(candidate => candidate.Partition == partition);
        return failedPartition is not null;
    }

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
        if (_resolvedPartitions.Count > MaxResolvedPartitions)
        {
            _resolvedPartitions.RemoveAt(0);
        }
        _partitions.Remove(failedPartition.Id);
    }

    /// <summary>
    /// Quarantine a failed partition, preventing further automatic retries.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to quarantine.</param>
    public void Quarantine(Key partition)
    {
        if (TryGet(partition, out var failedPartition))
        {
            failedPartition.IsQuarantined = true;
        }
    }

    void Add(FailedPartition failedPartition) => _partitions[failedPartition.Id] = failedPartition;
}
