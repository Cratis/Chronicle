// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Recovering every failed partition an observer remembers.
/// </summary>
/// <remarks>
/// Recovering a partition starts a job for it through the jobs manager, which reads the observer's jobs from storage
/// and then prepares and starts the job. For an observer that has accumulated hundreds of failed partitions, doing all
/// of them in one go keeps the observer busy for longer than a grain call's response timeout - and while it is busy,
/// nothing else reaches it: not a client's subscribe, not the unsubscribe of the connection it replaces, not a failed
/// partition being reported. A client whose subscribe times out retries it, and every subscribe used to ask for a full
/// recovery of its own on top of those still waiting, so after a reconnect the observer never caught up with the work
/// queued on it and stayed disconnected.
/// <para>
/// So a request to recover is remembered rather than acted on immediately, however many times it is made, and the
/// work is done a bounded batch per turn, letting whatever else is waiting for the observer in between.
/// </para>
/// </remarks>
public partial class Observer
{
    /// <summary>
    /// The most failed partitions the observer starts recovering in a single turn.
    /// </summary>
    internal const int MaxPartitionRecoveriesPerTurn = 10;

    static readonly TimeSpan _recoveryStartFailureDelay = TimeSpan.FromSeconds(5);

    readonly Queue<FailedPartitionId> _partitionsAwaitingRecovery = new();
    readonly HashSet<FailedPartitionId> _partitionsAwaitingRecoveryLookup = [];
    bool _recoveryOfAllFailedPartitionsRequested;
    bool _partitionRecoveryTurnScheduled;

    /// <inheritdoc/>
    public async Task TryRecoverAllFailedPartitions()
    {
        if (IsRetired || _removed) return;
        await ThrowIfKeyDoesNotOwnDefinition();

        await ReconcilePartitionDeferrals();
        _recoveryOfAllFailedPartitionsRequested = true;
        _retryJobSweepRequested = true;
        SchedulePartitionRecoveryTurn();
    }

    void SchedulePartitionRecoveryTurn(TimeSpan delay = default)
    {
        if (_partitionRecoveryTurnScheduled)
        {
            return;
        }

        _partitionRecoveryTurnScheduled = true;
        this.ScheduleInSeparateTurn(RecoverNextBatchOfFailedPartitions, delay);
    }

    async Task RecoverNextBatchOfFailedPartitions()
    {
        _partitionRecoveryTurnScheduled = false;
        if (IsRetired || _removed || !CanRecoverFailedPartitions())
        {
            ForgetPartitionsAwaitingRecovery();
            return;
        }

        // The sweep and the partition recoveries of this turn share one budget of job drops.
        _retryJobDropBudget = MaxRetryJobDropsPerTurn;
        try
        {
            await RecoverBatchOfFailedPartitions();
        }
        finally
        {
            _retryJobDropBudget = null;
        }
    }

    async Task RecoverBatchOfFailedPartitions()
    {
        if (_recoveryOfAllFailedPartitionsRequested)
        {
            _recoveryOfAllFailedPartitionsRequested = false;
            foreach (var partition in Failures.Partitions.Where(_ => !_.IsQuarantined && _partitionsAwaitingRecoveryLookup.Add(_.Id)))
            {
                _partitionsAwaitingRecovery.Enqueue(partition.Id);
            }
        }

        if (_retryJobSweepRequested)
        {
            _retryJobSweepRequested = await DropRetryJobsThatCanNeverRun();
        }

        var config = await GetRetryConfiguration();
        var started = 0;
        var failedToStart = false;
        var notStarted = new List<FailedPartitionId>();
        while (started < MaxPartitionRecoveriesPerTurn && _partitionsAwaitingRecovery.TryDequeue(out var failedPartitionId))
        {
            _partitionsAwaitingRecoveryLookup.Remove(failedPartitionId);

            // The partition may have recovered, been cleared or been quarantined since it was queued.
            var partition = Failures.Partitions.FirstOrDefault(_ => _.Id == failedPartitionId && !_.IsQuarantined);
            if (partition is null)
            {
                continue;
            }

            var attemptCount = partition.AttemptsInCurrentBudget;
            if (config.MaxRetryAttempts > 0 && attemptCount > config.MaxRetryAttempts)
            {
                logger.SkippingRecoveryMaxAttemptsExceeded(partition.Partition, attemptCount, config.MaxRetryAttempts);
                continue;
            }

            if (attemptCount > 0)
            {
                logger.StartingRecoveryWithExistingAttempts(partition.Partition, attemptCount, config.MaxRetryAttempts);
            }

            try
            {
                await StartRecoverJobForFailedPartition(partition);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !IsRetired)
            {
                // A job that cannot be started - its grain could not activate while the silo was busy - must not end the
                // turn: the partitions still queued would wait for the next subscription, which may never come.
                logger.CouldNotStartRecoveryOfFailedPartition(ex, partition.Partition);
                notStarted.Add(failedPartitionId);
                failedToStart = true;
            }

            started++;
        }

        // Tried again in a later turn, not in this one: it would only fail the same way again straight away.
        foreach (var failedPartitionId in notStarted.Where(_partitionsAwaitingRecoveryLookup.Add))
        {
            _partitionsAwaitingRecovery.Enqueue(failedPartitionId);
        }

        if (_partitionsAwaitingRecovery.Count > 0 || _retryJobSweepRequested)
        {
            logger.DeferringRecoveryOfFailedPartitions(_partitionsAwaitingRecovery.Count);
            SchedulePartitionRecoveryTurn(failedToStart ? _recoveryStartFailureDelay : TimeSpan.Zero);
        }
    }

    bool CanRecoverFailedPartitions()
    {
        if (IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return false;
        }

        // A recovery job prepares against the observer's subscriber. With none, every job fails to prepare and the
        // partitions stay failed - the next subscribe asks for their recovery again.
        if (!_subscription.IsSubscribed)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsNotSubscribed();
            return false;
        }

        return true;
    }

    void ForgetPartitionsAwaitingRecovery()
    {
        _recoveryOfAllFailedPartitionsRequested = false;
        _retryJobSweepRequested = false;
        _partitionsAwaitingRecovery.Clear();
        _partitionsAwaitingRecoveryLookup.Clear();
    }
}
