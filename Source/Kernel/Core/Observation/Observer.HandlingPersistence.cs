// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    const int MaxPendingHandledCountPartitions = 512;

    readonly HandledEventsReportDeduplication _handledEventsReports = new();

    bool HasPendingCoalescedWrites => _pendingHandledCounts.Count > 0 || _pendingLivePartitionCounts.Count > 0 || _failureStateWritePending;

    /// <inheritdoc/>
    /// <remarks>
    /// The counts are merged into what is pending for the partition and event type and the call returns, so a burst of
    /// reports from many job steps does not turn into a burst of storage writes inside this non-reentrant grain. They
    /// are made durable together by <see cref="FlushPendingHandledCounts"/> after at most
    /// <see cref="Configuration.Observers.WriteCoalescingInterval"/> seconds, or sooner once
    /// <see cref="MaxPendingHandledCountPartitions"/> partitions are pending. The call never waits for the flush; only
    /// when write coalescing is turned off does it make the counts durable before returning.
    /// A report a step sends again after a timeout is recognized by <see cref="HandledEventsReportDeduplication"/>
    /// and not taken twice.
    /// </remarks>
    public async Task ReportHandledEvents(Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType, JobStepId reportedBy, EventSequenceNumber handledUpTo)
    {
        Skips.JobReported(partition);
        if (CannotHandleEvents) return;
        if (!_handledEventsReports.TryTake(reportedBy, partition, handledUpTo)) return;

        MergeHandledCounts(partition, countsPerEventType);
        await FlushOrScheduleHandledCounts();
    }

    /// <inheritdoc/>
    public async Task ReportHandledEventsBatch(JobStepId reportedBy, IReadOnlyList<Jobs.HandledEventsReport> reports)
    {
        foreach (var report in reports)
        {
            Skips.JobReported(report.Partition);
        }

        if (CannotHandleEvents) return;
        foreach (var report in reports)
        {
            if (_handledEventsReports.TryTake(reportedBy, report.Partition, report.HandledUpTo))
            {
                MergeHandledCounts(report.Partition, report.Counts);
            }
        }

        await FlushOrScheduleHandledCounts();
    }

    static void MergeCounts(Dictionary<EventTypeId, EventCount> pending, IReadOnlyDictionary<EventTypeId, EventCount> counts)
    {
        foreach (var (eventTypeId, count) in counts)
        {
            pending[eventTypeId] = pending.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
        }
    }

    void MergeHandledCounts(Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0) return;
        if (!_pendingHandledCounts.TryGetValue(partition, out var pending))
        {
            pending = [];
            _pendingHandledCounts[partition] = pending;
        }
        MergeCounts(pending, countsPerEventType);
    }

    async Task FlushOrScheduleHandledCounts()
    {
        if (_writeCoalescingInterval <= TimeSpan.Zero)
        {
            await FlushPendingHandledCounts();
            return;
        }

        if (_pendingHandledCounts.Count >= MaxPendingHandledCountPartitions ||
            _pendingLivePartitionCounts.Count >= MaxPendingHandledCountPartitions)
        {
            BringCoalescedWriteForward();
            return;
        }

        EnsureCoalescedWriteTimer();
    }

    /// <summary>
    /// Makes the handled-event counts reported since the last flush durable, with one state write for all of them.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FlushPendingHandledCounts()
    {
        await FlushPendingLivePartitionCounts();
        if (_pendingHandledCounts.Count == 0) return;

        // Serialize independent handled-count writes with namespace resource deletion as well as source writes.
        await _alertMutationLock.WaitAsync();
        try
        {
            if (CannotHandleEvents)
            {
                _pendingHandledCounts.Clear();
                return;
            }

            if (await RelinquishOwnershipIfMoved()) return;

            _handledEventsReports.ForgetQuietSteps();
            var pending = _pendingHandledCounts;
            _pendingHandledCounts = [];
            var written = new HashSet<Key>();
            try
            {
                var countsStorage = GetObserverHandledCountsStorage();
                foreach (var chunk in pending.Chunk(MaxPendingHandledCountPartitions))
                {
                    await countsStorage.Increment(_observerId, chunk
                        .Select(_ => new KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>(_.Key, _.Value))
                        .ToArray());
                    RecordWrittenCountChunk(chunk, written);
                }

                await WriteStateAsync();
            }
            catch
            {
                // What was not made durable goes back to be flushed with the next batch, so no count is lost.
                RestorePendingHandledCounts(pending.Where(_ => !written.Contains(_.Key)));
                throw;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    void RecordWrittenCountChunk(KeyValuePair<Key, Dictionary<EventTypeId, EventCount>>[] chunk, HashSet<Key> written)
    {
        foreach (var (partition, counts) in chunk)
        {
            written.Add(partition);
            State = WithIncrementedRunningTotals(State, counts);
            RecordedHandlingState();
        }
    }

    void BringCoalescedWriteForward()
    {
        if (_coalescedWriteBroughtForward) return;

        _coalescedWriteTimer?.Dispose();
        _coalescedWriteBroughtForward = true;
        _coalescedWriteTimer = this.RegisterGrainTimer(
            FlushCoalescedWritesFromTimer,
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    void EnsureCoalescedWriteTimer()
    {
        if (_coalescedWriteTimer is not null || !HasPendingCoalescedWrites) return;

        _coalescedWriteTimer = this.RegisterGrainTimer(
            FlushCoalescedWritesFromTimer,
            new GrainTimerCreationOptions { DueTime = _writeCoalescingInterval, Period = Timeout.InfiniteTimeSpan });
    }

    /// <summary>
    /// Registers the retry reminders and writes the observer state for the partition failures recorded since the last flush.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FlushPendingFailures()
    {
        if (!_failureStateWritePending && _pendingRetryReminders.Count == 0) return;

        await _alertMutationLock.WaitAsync();
        try
        {
            if (await RelinquishOwnershipIfMoved()) return;

            var reminders = _pendingRetryReminders;
            _pendingRetryReminders = [];
            _failureStateWritePending = false;
            if (CannotHandleEvents) return;

            try
            {
                foreach (var (failureId, retryDelay) in reminders)
                {
                    if (ShouldRegisterRetryReminder(failureId))
                    {
                        await this.RegisterOrUpdateReminder(PartitionReminderName(failureId), retryDelay, GetRetryReminderPeriod(retryDelay));
                    }
                }

                await WriteStateAsync();
            }
            catch
            {
                RestoreRetryReminders(reminders);
                throw;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    bool ShouldRegisterRetryReminder(FailedPartitionId failureId) =>
        !IsQuarantined && Failures.Partitions.Any(_ => _.Id == failureId && !_.IsQuarantined);

    void RestoreRetryReminders(Dictionary<FailedPartitionId, TimeSpan> reminders)
    {
        foreach (var (failureId, retryDelay) in reminders)
        {
            _pendingRetryReminders.TryAdd(failureId, retryDelay);
        }
        _failureStateWritePending = true;
    }

    async Task FlushCoalescedWritesFromTimer(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _coalescedWriteTimer?.Dispose();
        _coalescedWriteTimer = null;
        _coalescedWriteBroughtForward = false;
        await FlushPendingHandledCounts();
        await FlushPendingFailures();
    }

    void RestorePendingHandledCounts(IEnumerable<KeyValuePair<Key, Dictionary<EventTypeId, EventCount>>> notWritten)
    {
        foreach (var (partition, counts) in notWritten)
        {
            if (!_pendingHandledCounts.TryGetValue(partition, out var pending))
            {
                _pendingHandledCounts[partition] = counts;
                continue;
            }
            MergeCounts(pending, counts);
        }
    }

    IObserverHandledCountsStorage GetObserverHandledCountsStorage() =>
        storage
            .GetEventStore(_observerKey.EventStore)
            .GetNamespace(_observerKey.Namespace)
            .ObserverHandledCounts;

    /// <summary>
    /// Persists a progress-only advance of <see cref="ObserverState.NextEventSequenceNumber"/> — the observer
    /// moving past a batch that held nothing it is subscribed to — subject to debouncing.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The in-memory state has already advanced when this is called; the write is deferred until
    /// <see cref="Configuration.Observers.StatePersistenceBatchInterval"/> progress-only batches have accumulated (any other
    /// state write flushes it sooner, and the watchdog and deactivation flush it on their own cadence). Because
    /// catch-up (<see cref="CatchUp"/>) always resumes from the last persisted <see cref="ObserverState.NextEventSequenceNumber"/>
    /// and observers are idempotent, a crash between debounced writes only re-scans events the observer had already
    /// skipped — no event is lost or handled twice. This deliberately relaxes per-batch durability of progress in
    /// exchange for removing the dominant write on selective observers.
    /// </remarks>
    async Task WriteProgressStateDebounced()
    {
        _debouncedProgressWrites++;
        if (_debouncedProgressWrites >= _statePersistenceBatchInterval)
        {
            await WriteStateAsync();
        }
    }

    /// <summary>
    /// Flushes a progress-only advance that debouncing has left unpersisted, writing the observer state only when
    /// there is a pending advance.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FlushDebouncedProgressState()
    {
        await FlushPendingHandledCounts();
        await FlushPendingFailures();
        if (_debouncedProgressWrites > 0)
        {
            await WriteStateAsync();
        }
    }

    void RememberHandledEventsReportsForTheResponseTimeout()
    {
        _handledEventsReports.Window = HandledEventsReportDeduplication.WindowFor(ResponseTimeout());
    }
}
