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
    readonly Dictionary<FailedPartitionId, Snapshot> _persisted = [];
    readonly HashSet<FailedPartitionId> _persistedRemovals = [];
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
    /// <param name="kind">What kind of thing went wrong on the attempt.</param>
    /// <returns>A <see cref="FailedPartition"/> instance.</returns>
    /// <remarks>
    /// An attempt that fails at a later event than the previous attempt did made progress, and starts a new retry
    /// budget (see <see cref="FailedPartition.AttemptsInCurrentBudget"/>). Only a partition that keeps failing at the
    /// same place spends its budget and is eventually quarantined.
    /// </remarks>
    public FailedPartition RegisterAttempt(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> messages,
        string stackTrace,
        FailureKind kind = FailureKind.Unknown)
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

        if (AdvancedSincePreviousAttempt(failure, sequenceNumber))
        {
            // The partition moved forward since the last attempt, so it is not stuck: the attempt starts a new retry
            // budget instead of spending the old one. A partition failing on the same event keeps counting.
            failure.AttemptsBeforeBudgetReset = failure.Attempts.Count();
        }

        failure.AddAttempt(new()
        {
            Occurred = DateTimeOffset.UtcNow,
            SequenceNumber = sequenceNumber,
            Messages = messages,
            StackTrace = stackTrace,
            Kind = kind
        });

        return failure;
    }

    /// <summary>
    /// Defer delivery of a partition without registering a failure attempt.
    /// </summary>
    /// <param name="partition">The partition whose delivery is deferred.</param>
    /// <param name="deferral">The durable deadline and inclusive resume position.</param>
    /// <returns>The unresolved partition episode.</returns>
    public FailedPartition Defer(Key partition, PartitionDeferral deferral)
    {
        if (!TryGet(partition, out var failure))
        {
            failure = new() { Partition = partition };
            Add(failure);
        }

        failure.Deferral = deferral;
        return failure;
    }

    /// <summary>
    /// Remove a failed partition.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to remove.</param>
    public void Remove(Key partition)
    {
        if (!TryGet(partition, out var failedPartition)) return;

        failedPartition.IsResolved = true;
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

    /// <summary>
    /// Clear the quarantine of a failed partition and start a new retry budget for it, keeping its attempt history.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to clear the quarantine for.</param>
    /// <returns>True when the partition was quarantined and is now cleared, false when it is unknown or was not quarantined.</returns>
    public bool ClearQuarantine(Key partition)
    {
        if (!TryGet(partition, out var failedPartition) || !failedPartition.IsQuarantined)
        {
            return false;
        }

        failedPartition.IsQuarantined = false;
        failedPartition.AttemptsBeforeBudgetReset = failedPartition.Attempts.Count();
        return true;
    }

    /// <summary>
    /// Gets what has to be written to storage for it to match what is held here: the partitions that are new or have changed since
    /// they were last persisted, and the resolved partitions that have not been removed from storage yet.
    /// </summary>
    /// <returns>A <see cref="FailedPartitions"/> holding only the changes, to hand to storage, then to <see cref="MarkPersisted(FailedPartitions)"/> once written.</returns>
    /// <remarks>
    /// Storage is handed everything an observer holds on every save, and a recovery saves several times per partition, so
    /// writing it all every time is quadratic in the number of failed partitions. This is independent of the storage target.
    /// </remarks>
    public FailedPartitions GetChangesSincePersisted()
    {
        var changes = new FailedPartitions();
        var changed = new List<FailedPartition>();
        foreach (var partition in _partitions.Values)
        {
            if (!_persisted.TryGetValue(partition.Id, out var persisted) || !persisted.Matches(partition))
            {
                changed.Add(partition);
            }
        }

        changes.Partitions = changed;
        foreach (var resolved in _resolvedPartitions)
        {
            if (!_persistedRemovals.Contains(resolved.Id))
            {
                changes._resolvedPartitions.Add(resolved);
            }
        }

        return changes;
    }

    /// <summary>
    /// Remember that changes obtained from <see cref="GetChangesSincePersisted"/> have been written to storage.
    /// </summary>
    /// <param name="changes">The changes that were written.</param>
    public void MarkPersisted(FailedPartitions changes)
    {
        foreach (var partition in changes.Partitions)
        {
            _persisted[partition.Id] = Snapshot.Of(partition);
            _persistedRemovals.Remove(partition.Id);
        }

        foreach (var resolved in changes.ResolvedPartitions)
        {
            _persisted.Remove(resolved.Id);
            _persistedRemovals.Add(resolved.Id);
        }

        Prune();
    }

    /// <summary>
    /// Remember that everything held here is what storage holds, as it is right after it was read from storage.
    /// </summary>
    public void MarkAllPersisted()
    {
        _persisted.Clear();
        _persistedRemovals.Clear();
        foreach (var partition in _partitions.Values)
        {
            _persisted[partition.Id] = Snapshot.Of(partition);
        }
    }

    /// <summary>
    /// Forget what is known to be in storage, so the next save writes everything held here.
    /// </summary>
    public void ForgetPersisted()
    {
        _persisted.Clear();
        _persistedRemovals.Clear();
    }

    static bool AdvancedSincePreviousAttempt(FailedPartition failure, EventSequenceNumber sequenceNumber)
    {
        var previous = failure.LastAttempt.SequenceNumber;
        return failure.Attempts.Any() &&
            previous.IsActualValue &&
            sequenceNumber.IsActualValue &&
            sequenceNumber > previous;
    }

    void Add(FailedPartition failedPartition) => _partitions[failedPartition.Id] = failedPartition;

    void Prune()
    {
        // Only what is still held, resolved or not, is worth remembering about.
        if (_persisted.Count > _partitions.Count)
        {
            foreach (var id in _persisted.Keys.Where(_ => !_partitions.ContainsKey(_)).ToArray())
            {
                _persisted.Remove(id);
            }
        }

        if (_persistedRemovals.Count > _resolvedPartitions.Count)
        {
            _persistedRemovals.IntersectWith(_resolvedPartitions.Select(_ => _.Id));
        }
    }

    /// <summary>
    /// What was persisted of a failed partition, kept to tell whether it has changed since. Equality is by value for what
    /// can change in place, and by reference for the attempts, which are only ever added.
    /// </summary>
    /// <param name="Partition">The partition.</param>
    /// <param name="ObserverId">The observer it belongs to.</param>
    /// <param name="AttemptCount">The number of attempts.</param>
    /// <param name="FirstAttempt">The first attempt.</param>
    /// <param name="LastAttempt">The last attempt.</param>
    /// <param name="IsResolved">Whether it is resolved.</param>
    /// <param name="IsQuarantined">Whether it is quarantined.</param>
    /// <param name="AttemptsBeforeBudgetReset">The attempts before the retry budget was reset.</param>
    /// <param name="RecoveryCursor">The recovery cursor.</param>
    /// <param name="RecoveryOwner">The recovery owner.</param>
    /// <param name="Hold">The hold.</param>
    /// <param name="Deferral">The deferral.</param>
    /// <param name="ReplayEpoch">The replay epoch.</param>
    /// <param name="WebhookDisposition">The webhook failure disposition.</param>
    /// <param name="WebhookRecoveryId">The webhook recovery identifier.</param>
    sealed record Snapshot(
        Key Partition,
        ObserverId ObserverId,
        int AttemptCount,
        FailedPartitionAttempt? FirstAttempt,
        FailedPartitionAttempt? LastAttempt,
        bool IsResolved,
        bool IsQuarantined,
        int AttemptsBeforeBudgetReset,
        FailedPartitionRecoveryCursor? RecoveryCursor,
        FailedPartitionRecoveryOwner? RecoveryOwner,
        FailedPartitionHold? Hold,
        PartitionDeferral? Deferral,
        ReplayEpoch? ReplayEpoch,
        WebhookFailureDisposition? WebhookDisposition,
        Guid? WebhookRecoveryId)
    {
        internal static Snapshot Of(FailedPartition failedPartition) => new(
            failedPartition.Partition,
            failedPartition.ObserverId,
            failedPartition.Attempts.Count(),
            failedPartition.Attempts.FirstOrDefault(),
            failedPartition.Attempts.LastOrDefault(),
            failedPartition.IsResolved,
            failedPartition.IsQuarantined,
            failedPartition.AttemptsBeforeBudgetReset,
            failedPartition.RecoveryCursor,
            failedPartition.RecoveryOwner,
            failedPartition.Hold,
            failedPartition.Deferral,
            failedPartition.ReplayEpoch,
            failedPartition.WebhookDisposition,
            failedPartition.WebhookRecoveryId);

        internal bool Matches(FailedPartition failedPartition) => Equals(Of(failedPartition));
    }
}
