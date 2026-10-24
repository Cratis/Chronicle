// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    const int MaxPendingHandledCountPartitions = 512;

    readonly HandledEventsReportDeduplication _handledEventsReports = new();

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
        using var span = activitySource.Handle();
        span?.Activity?.Tag(_observerKey);
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        if (!events.Any())
        {
            return;
        }

        if (!ShouldHandleEvent(partition))
        {
            NoteHeldBackEvents(partition, events);
            return;
        }

        try
        {
            await HandleEventsForPartition(partition, events);
        }
        finally
        {
            EvaluateSuspectedSkips();
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
            if (perEventType.TryGetValue(eventTypeId, out var existing))
            {
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

        if (_pendingHandledCounts.Count >= MaxPendingHandledCountPartitions)
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
        if (_pendingHandledCounts.Count == 0)
        {
            return;
        }

        // Serialize independent handled-count writes with namespace resource deletion as well as source writes.
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !_observerExists)
            {
                _pendingHandledCounts.Clear();
                return;
            }

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
                    foreach (var (partition, counts) in chunk)
                    {
                        written.Add(partition);
                        State = WithIncrementedRunningTotals(State, counts);
                        RecordedHandlingState();
                    }
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
        if (_coalescedWriteTimer is not null || (_pendingHandledCounts.Count == 0 && !_failureStateWritePending))
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
            var reminders = _pendingRetryReminders;
            _pendingRetryReminders = [];
            _failureStateWritePending = false;
            if (IsRetired || _removed || !_observerExists)
            {
                return;
            }

            try
            {
                foreach (var (failureId, retryDelay) in reminders)
                {
                    if (!IsQuarantined && Failures.Partitions.Any(_ => _.Id == failureId && !_.IsQuarantined))
                    {
                        await this.RegisterOrUpdateReminder(PartitionReminderName(failureId), retryDelay, GetRetryReminderPeriod(retryDelay));
                    }
                }

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

    async Task HandleEventsForPartition(Key partition, IEnumerable<AppendedEvent> events)
    {
        var observedTailEventSequenceNumber = events.Last().Context.SequenceNumber;

        // An observer subscribed to all events has no fixed event type list to match against - every event is
        // of interest to it by definition, so this "none of these events are ones we're subscribed to" fast
        // path never applies to it.
        if (!State.SubscribesToAllEvents && !events.Any(_ => _subscription.EventTypes.Any(et => et.Id == _.Context.EventType.Id)))
        {
            State = State with
            {
                NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                TailEventSequenceNumber = observedTailEventSequenceNumber
            };
            RecordedHandlingState();
            await WriteProgressStateDebounced();
            return;
        }

        if (_subscription.Filters is { } filters)
        {
            if (filters.EventSourceType is { } eventSourceType &&
                eventSourceType != EventSourceType.Unspecified &&
                !events.Any(_ => _.Context.EventSourceType == eventSourceType))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                RecordedHandlingState();
                await WriteProgressStateDebounced();
                return;
            }

            if (filters.EventStreamType is { } eventStreamType &&
                !eventStreamType.IsAll &&
                !events.Any(_ => _.Context.EventStreamType == eventStreamType))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                RecordedHandlingState();
                await WriteProgressStateDebounced();
                return;
            }

            if (filters.Tags.Any() &&
                !events.Any(_ => _.Context.Tags.Any(t => filters.Tags.Contains(t.Value))))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                RecordedHandlingState();
                await WriteProgressStateDebounced();
                return;
            }
        }

        var failed = false;
        var failureKind = FailureKind.Unknown;
        var failurePolicy = WebhookDeliveryFailurePolicy.Retry;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        var eventsToHandle = events.Where(_ => _.Context.SequenceNumber >= tailEventSequenceNumber).ToArray();
        NoteDiscardedEvents(partition, events, tailEventSequenceNumber);
        if (await ExceedsLiveBudget(eventsToHandle.Length) && await HandOverLargeLiveBatchToCatchUp(partition, eventsToHandle))
        {
            return;
        }

        var numEventsSuccessfullyHandled = EventCount.Zero;
        var stateChanged = false;
        IReadOnlyDictionary<EventTypeId, EventCount> handledCountsPerEventType = ImmutableDictionary<EventTypeId, EventCount>.Empty;
        if (await CanDispatchDelivery(partition, eventsToHandle))
        {
            // Record this partition as in-flight on the observer state and make it durable *before* the subscriber
            // is invoked. If the silo dies between this point and the post-handling state write, the marker survives
            // on the reloaded observer state and drives partition catch-up on the next activation.
            DeliveryFenceContext? deliveryFence = null;
            var inFlightPartitions = new HashSet<Key>(State.InFlightPartitions) { partition };
            State = State with { InFlightPartitions = inFlightPartitions };
            RecordedHandlingState();
            await WriteStateAsync();

            using (new WriteSuspension(this))
            {
                try
                {
                    var firstEvent = eventsToHandle[0];
                    tailEventSequenceNumber = firstEvent.Context.SequenceNumber;
                    var decryptedEvents = await DecryptEvents(eventsToHandle);

                    var subscriberTimeout = GetLiveSubscriberTimeout(await configurationProvider.GetSubscriberTimeoutForObserver(_observerKey));
                    var hold = await GetClientDeliveryHold();

                    ObserverSubscriberResult result;
                    while (true)
                    {
                        var target = subscriberSelector.Select(_subscription, partition);

                        // A webhook partition has one non-interleavable activation across live delivery and jobs,
                        // independent of which silo owns the observer or executes the job.
                        var siloAddress = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType)
                            ? SiloAddress.Zero
                            : target.SiloAddress;
                        var key = _subscription.GetSubscriberKeyFor(partition, siloAddress);

                        var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                        var context = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments).WithDeadlines(subscriberTimeout, hold, _clock);
                        deliveryFence = AbandonedDeliveryDrain.Capture(_subscription, partition, target, context);
                        result = await subscriber.OnNextWithin(subscriberTimeout, partition, decryptedEvents, context);

                        // A disconnected result from one client instance only removes that instance —
                        // the batch is retried against the remaining instances. Only when the last
                        // instance is gone does the observer unsubscribe (below).
                        if (result.State == ObserverSubscriberState.Disconnected &&
                            target.ConnectedClient is not null &&
                            _subscription.Targets.Count > 1)
                        {
                            logger.ClientInstanceDisconnectedRetryingWithRemaining(target.ConnectedClient.ConnectionId, partition);
                            RemoveSubscriberTarget(target);
                            continue;
                        }

                        break;
                    }

                    numEventsSuccessfullyHandled = result.HandledAnyEvents
                        ? eventsToHandle.Count(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation)
                        : EventCount.Zero;

                    if (result.State == ObserverSubscriberState.Deferred)
                    {
                        await PartitionDeferred(partition, result.Deferral!);
                        stateChanged = true;
                    }
                    else if (result.State == ObserverSubscriberState.Failed)
                    {
                        failed = true;
                        failureKind = FailureKind.Handling;
                        failurePolicy = result.FailurePolicy;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        tailEventSequenceNumber = result.HandledAnyEvents
                            ? result.LastSuccessfulObservation
                            : firstEvent.Context.SequenceNumber;
                    }
                    else if (result.State == ObserverSubscriberState.Disconnected)
                    {
                        // The batch is not advanced, so whatever subscription takes over catches up from the same position.
                        await PromoteSupersededRegistrationOrUnsubscribe();
                        stateChanged = true;
                    }

                    if (numEventsSuccessfullyHandled > 0)
                    {
                        stateChanged = true;
                        State = State with
                        {
                            NextEventSequenceNumber = result.LastSuccessfulObservation.Next(),
                            TailEventSequenceNumber = observedTailEventSequenceNumber
                        };
                        var previousLastHandled = State.LastHandledEventSequenceNumber;
                        var shouldSetLastHandled =
                            previousLastHandled == EventSequenceNumber.Unavailable ||
                            previousLastHandled < result.LastSuccessfulObservation;
                        State = State with
                        {
                            LastHandledEventSequenceNumber = shouldSetLastHandled
                                ? result.LastSuccessfulObservation
                                : previousLastHandled,
                        };

                        var handledEvents = decryptedEvents.Where(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation);
                        handledCountsPerEventType = handledEvents.CountByEventType();
                        State = WithIncrementedRunningTotals(State, handledCountsPerEventType);
                        RecordedHandlingState();
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    failureKind = ex.ToFailureKind();
                    exceptionMessages = ex.GetAllMessages().ToArray();
                    exceptionStackTrace = ex.StackTrace ?? string.Empty;
                }
            }

            try
            {
                // The in-flight marker has served its purpose now that the outcome is known, so clear it before the
                // outcome is persisted — whichever state write happens below carries the removal. A partition that
                // failed is recovered through FailedPartitions storage, so keeping its marker would only force a
                // redundant catch-up for events we already know about.
                var remainingInFlight = new HashSet<Key>(State.InFlightPartitions);
                remainingInFlight.Remove(partition);
                State = State with { InFlightPartitions = remainingInFlight };
                RecordedHandlingState();

                if (failed)
                {
                    await RecordPartitionFailure(partition, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace, failureKind, null, null, new(failurePolicy, deliveryFence));
                }
                else
                {
                    _metrics?.SuccessfulObservation();
                }

                if (stateChanged)
                {
                    await WriteStateAsync();
                }

                if (handledCountsPerEventType.Count > 0)
                {
                    await GetObserverHandledCountsStorage().Increment(_observerId, partition, handledCountsPerEventType);
                }
            }
            catch (Exception ex)
            {
                logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
            }
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

        if (State.ReplayingPartitions.Contains(partition))
        {
            logger.PartitionReplayingCannotHandleNewEvents(partition);
            return false;
        }

        if (State.CatchingUpPartitions.Contains(partition))
        {
            logger.PartitionCatchingUpCannotHandleNewEvents(partition);
            return false;
        }

        return true;
    }

    void HandleNewLastHandledEvent(EventSequenceNumber lastHandledEvent)
    {
        if (!lastHandledEvent.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }

        var newLastHandledEvent = State.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ||
                                  State.LastHandledEventSequenceNumber < lastHandledEvent ? lastHandledEvent : State.LastHandledEventSequenceNumber;
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
        var messaging = ServiceProvider.GetService<IOptions<SiloMessagingOptions>>()?.Value ?? new SiloMessagingOptions();
        var responseTimeout = messaging.ResponseTimeout > messaging.SystemResponseTimeout ? messaging.ResponseTimeout : messaging.SystemResponseTimeout;
        _handledEventsReports.Window = HandledEventsReportDeduplication.WindowFor(responseTimeout);
    }
}
