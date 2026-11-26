// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    const int MaxPendingHandledCountPartitions = 512;

    readonly HandledEventsReportDeduplication _handledEventsReports = new();
    ObserverSubscription? _eventTypeSubscription;
    FrozenSet<EventTypeId> _subscribedEventTypeIds = [];

    /// <inheritdoc/>
    public Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (IsRetired || _removed || !_observerExists) return Task.CompletedTask;
        State = State with
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber
        };
        RecordedHandlingState();

        return WriteStateAsync();
    }

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
        Skips.Delivered(partition, handledUpTo);
        CompleteHeldBackDelivery(partition, handledUpTo);
        if (IsRetired || _removed || !_observerExists) return;
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
            Skips.Delivered(report.Partition, report.HandledUpTo);
            CompleteHeldBackDelivery(report.Partition, report.HandledUpTo);
        }

        if (IsRetired || _removed || !_observerExists) return;
        foreach (var report in reports)
        {
            if (_handledEventsReports.TryTake(reportedBy, report.Partition, report.HandledUpTo))
            {
                MergeHandledCounts(report.Partition, report.Counts);
            }
        }

        await FlushOrScheduleHandledCounts();
    }

    /// <inheritdoc/>
    public async Task Handle(Key partition, IEnumerable<AppendedEvent> events)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!events.Any())
        {
            return;
        }

        if (!ShouldHandleEvent(partition))
        {
            RememberHeldBackDeliveries(partition, events);
            NoteHeldBackEvents(partition, events);
            return;
        }

        NoteHandledAfterWake();
        using var span = activitySource.Handle();
        span?.Activity?.Tag(_observerKey);
        span?.Activity?.Tag(Definition.Type);
        if (span?.Activity is { IsAllDataRequested: true } activity && events.TryGetNonEnumeratedCount(out var eventCount))
        {
            activity.TagEventCount(eventCount);
        }

        try
        {
            await HandleEventsForPartition(partition, events, span?.Activity);
        }
        catch (Exception ex)
        {
            span?.Activity?.RecordFailure(ex);
            throw;
        }
        finally
        {
            await EvaluateAndRepairMissedDeliveries();
        }
    }

    /// <summary>
    /// Returns a new <see cref="ObserverState"/> with the running handled-event totals incremented by the
    /// counts of a single handled batch. The per-partition breakdown lives in the dedicated
    /// <see cref="IObserverHandledCountsStorage"/> and is not part of <see cref="ObserverState"/>.
    /// </summary>
    /// <param name="state">The current <see cref="ObserverState"/> to update.</param>
    /// <param name="countsPerEventType">The number of events handled in the batch, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>A new <see cref="ObserverState"/> with <see cref="ObserverState.HandledEventCount"/> and <see cref="ObserverState.HandledEventCountPerEventType"/> incremented accordingly.</returns>
    static ObserverState WithIncrementedRunningTotals(
        ObserverState state,
        IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return state;
        }

        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var total = 0UL;
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            total += count.Value;
            perEventType[eventTypeId] = perEventType.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
        }

        return state with
        {
            HandledEventCount = state.HandledEventCount + total,
            HandledEventCountPerEventType = perEventType
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObserverState"/> with the given partition's contribution subtracted from the
    /// running handled-event totals. Used when a partition replay begins; the partition's counts come from the
    /// dedicated <see cref="IObserverHandledCountsStorage"/>.
    /// </summary>
    /// <param name="state">The current <see cref="ObserverState"/> to update.</param>
    /// <param name="partitionCounts">The partition's handled-event counts, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>A new <see cref="ObserverState"/> with the partition's counts subtracted from the aggregates.</returns>
    static ObserverState WithSubtractedPartitionHandledEventCounts(
        ObserverState state,
        Dictionary<EventTypeId, EventCount> partitionCounts)
    {
        if (partitionCounts.Count == 0)
        {
            return state;
        }

        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var totalForPartition = 0UL;
        foreach (var (eventTypeId, count) in partitionCounts)
        {
            totalForPartition += count.Value;
            SubtractHandledCount(perEventType, eventTypeId, count);
        }

        var newTotal = state.HandledEventCount.Value > totalForPartition
            ? state.HandledEventCount.Value - totalForPartition
            : 0UL;

        return state with
        {
            HandledEventCount = newTotal,
            HandledEventCountPerEventType = perEventType
        };
    }

    static void SubtractHandledCount(Dictionary<EventTypeId, EventCount> perEventType, EventTypeId eventTypeId, EventCount count)
    {
        if (!perEventType.TryGetValue(eventTypeId, out var existing)) return;
        var newCount = existing.Value > count.Value ? existing.Value - count.Value : 0UL;
        if (newCount == 0)
        {
            perEventType.Remove(eventTypeId);
        }
        else
        {
            perEventType[eventTypeId] = newCount;
        }
    }

    static string FailureKindCode(FailureKind kind) => kind switch
    {
        FailureKind.Handling => "handling",
        FailureKind.Timeout => "timeout",
        FailureKind.Disconnected => "disconnected",
        FailureKind.WaitingForHandler => "waiting_for_handler",
        _ => "unknown"
    };

    void MergeHandledCounts(Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count > 0)
        {
            if (!_pendingHandledCounts.TryGetValue(partition, out var pending))
            {
                pending = [];
                _pendingHandledCounts[partition] = pending;
            }

            foreach (var (eventTypeId, count) in countsPerEventType)
            {
                pending[eventTypeId] = pending.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
            }
        }
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

    void BringCoalescedWriteForward()
    {
        if (_coalescedWriteBroughtForward)
        {
            return;
        }

        _coalescedWriteTimer?.Dispose();
        _coalescedWriteBroughtForward = true;
        _coalescedWriteTimer = this.RegisterGrainTimer(
            FlushCoalescedWritesFromTimer,
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    void EnsureCoalescedWriteTimer()
    {
        if (_coalescedWriteTimer is not null ||
            (_pendingHandledCounts.Count == 0 && _pendingLivePartitionCounts.Count == 0 && !_failureStateWritePending))
        {
            return;
        }

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
        if (!_failureStateWritePending && _pendingRetryReminders.Count == 0)
        {
            return;
        }

        await _alertMutationLock.WaitAsync();
        try
        {
            if (await RelinquishOwnershipIfMoved()) return;

            var reminders = _pendingRetryReminders;
            _pendingRetryReminders = [];
            _failureStateWritePending = false;
            if (IsRetired || _removed || !_observerExists)
            {
                return;
            }

            try
            {
                await RegisterPendingRetryReminders(reminders);
                await WriteStateAsync();
            }
            catch
            {
                foreach (var (failureId, retryDelay) in reminders)
                {
                    _pendingRetryReminders.TryAdd(failureId, retryDelay);
                }

                _failureStateWritePending = true;
                throw;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task RegisterPendingRetryReminders(Dictionary<FailedPartitionId, TimeSpan> reminders)
    {
        foreach (var (failureId, retryDelay) in reminders)
        {
            if (!IsQuarantined && Failures.Partitions.Any(_ => _.Id == failureId && !_.IsQuarantined))
            {
                await this.RegisterOrUpdateReminder(PartitionReminderName(failureId), retryDelay, GetRetryReminderPeriod(retryDelay));
            }
        }
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

            foreach (var (eventTypeId, count) in counts)
            {
                pending[eventTypeId] = pending.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
            }
        }
    }

    FrozenSet<EventTypeId> SubscribedEventTypeIds()
    {
        if (!ReferenceEquals(_eventTypeSubscription, _subscription))
        {
            _subscribedEventTypeIds = _subscription.EventTypes.Select(static eventType => eventType.Id).ToFrozenSet();
            _eventTypeSubscription = _subscription;
        }

        return _subscribedEventTypeIds;
    }

    ObserverRunPreparationContext ContextForRunPreparation()
    {
        var subscribesToAllEvents = State.SubscribesToAllEvents;
        return new(
            _pageFloor ?? State.NextEventSequenceNumber,
            subscribesToAllEvents,
            subscribesToAllEvents ? _subscribedEventTypeIds : SubscribedEventTypeIds(),
            _subscription.Filters);
    }

    void RecordInapplicableRun(Key partition, EventSequenceNumber observedTail)
    {
        State = State with
        {
            NextEventSequenceNumber = CursorBeforePending(partition, CursorAfter(observedTail.Next())),
            TailEventSequenceNumber = TailAfter(observedTail)
        };
        RecordedHandlingState();
    }

    async Task HandleEventsForPartition(Key partition, IEnumerable<AppendedEvent> events, Activity? activity)
    {
        var preparationContext = ContextForRunPreparation();
        var prepared = ObserverRunPreparation.Prepare(events, preparationContext);
        var observedTailEventSequenceNumber = prepared.ObservedTail;

        // An observer subscribed to all events has no fixed event type list to match against - every event is
        // of interest to it by definition, so this "none of these events are ones we're subscribed to" fast
        // path never applies to it.
        if (!prepared.IsApplicable)
        {
            RecordInapplicableRun(partition, observedTailEventSequenceNumber);
            await WriteProgressStateDebounced();
            return;
        }

        var tailEventSequenceNumber = preparationContext.NextEventSequenceNumber;

        var eventsToHandle = prepared.EventsToHandle;
        if (await RepairDiscardedDeliveries(partition, events, tailEventSequenceNumber)) return;
        var configuration = await configurationProvider.GetFor(_observerKeyText);
        if (ExceedsLiveBudget(configuration, eventsToHandle.Length) && await HandOverLargeLiveBatchToCatchUp(partition, eventsToHandle))
        {
            return;
        }

        if (await CanDispatchDelivery(partition, eventsToHandle))
        {
            // Only a delivery that reaches the subscriber can race a catch-up; a hand-over holds its partition instead.
            using var delivery = BeginLiveDelivery();

            // Record this partition as in-flight on the observer state and make it durable *before* the subscriber
            // is invoked. If the silo dies between this point and the post-handling state write, the marker survives
            // on the reloaded observer state and drives partition catch-up on the next activation.
            var inFlightPartitions = new HashSet<Key>(State.InFlightPartitions) { partition };
            State = State with { InFlightPartitions = inFlightPartitions };
            RecordedHandlingState();
            await WriteStateAsync();
            if (!_observerExists) return;

            var batch = new LiveHandlingBatch(partition, eventsToHandle, observedTailEventSequenceNumber, tailEventSequenceNumber, configuration);
            var outcome = await HandleLiveDelivery(batch);
            await SettleLiveDelivery(partition, outcome, activity);
        }
    }

    IObserverHandledCountsStorage GetObserverHandledCountsStorage() =>
        storage
            .GetEventStore(_observerKey.EventStore)
            .GetNamespace(_observerKey.Namespace)
            .ObserverHandledCounts;

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        if (_subscription.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            return await (eventGenerationRelease ?? throw new EventGenerationDeliveryNotSupported()).Release(_observerKey.EventStore, _subscription.EventTypes, _eventTypeSchemas, events);
        }

        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        await storage.GetEventStore(_observerKey.EventStore).EventTypes.EnsureSchemasFor(_eventTypeSchemas, eventsToDecrypt);
        return await eventCompliance.Release(eventsToDecrypt, _eventTypeSchemas);
    }

    bool ShouldHandleEvent(Key partition)
    {
        if (IsRetired || _removed || !_observerExists) return false;
        if (!_subscription.IsSubscribed)
        {
            logger.ObserverIsNotSubscribed();
            return false;
        }

        if (Failures.IsFailed(partition))
        {
            logger.PartitionIsFailed(partition);
            return false;
        }

        if (IsQuarantined || State.RunningState != ObserverRunningState.Active)
        {
            logger.ObserverIsNotActive();
            return false;
        }

        if (_isPreparingCatchup)
        {
            logger.ObserverIsPreparingCatchup();
            return false;
        }

        return !IsPartitionHeldBack(partition);
    }

    bool IsPartitionHeldBack(Key partition)
    {
        if (State.ReplayingPartitions.Contains(partition))
        {
            logger.PartitionReplayingCannotHandleNewEvents(partition);
            return true;
        }

        if (State.CatchingUpPartitions.Contains(partition))
        {
            logger.PartitionCatchingUpCannotHandleNewEvents(partition);
            return true;
        }

        // The walk reads every partition to the end of the log, including one whose first event arrives after it registered
        // its partitions; delivering that event live as well would hand it to the subscriber twice.
        if (_walkingEveryPartition)
        {
            logger.CatchUpWalkCoversNewEvents(partition);
            return true;
        }

        return false;
    }

    void HandleNewLastHandledEvent(EventSequenceNumber lastHandledEvent)
    {
        if (!lastHandledEvent.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }

        var advancesLastHandled = State.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ||
                                  State.LastHandledEventSequenceNumber < lastHandledEvent;
        var newLastHandledEvent = advancesLastHandled ? lastHandledEvent : State.LastHandledEventSequenceNumber;
        var nextEventSequenceNumber = State.NextEventSequenceNumber <= lastHandledEvent ? lastHandledEvent.Next() : State.NextEventSequenceNumber;
        State = State with
        {
            LastHandledEventSequenceNumber = newLastHandledEvent,
            NextEventSequenceNumber = nextEventSequenceNumber
        };
        RecordedHandlingState();
    }

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
