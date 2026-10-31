// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Placement;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
[KeepAlive]
[PreferLocalPlacement]
public class AppendedEventsQueue : Grain, IAppendedEventsQueue, IDisposable
{
    /// <summary>
    /// Upper bound on how many queued batches a single dispatch coalesces before delivering. Bounds the size of the
    /// merged batch (and therefore the work of one dispatch); any remaining backlog is drained on the next loop pass.
    /// </summary>
    const int MaxCoalescedBatchesPerDispatch = 100;

    /// <summary>
    /// How many times a spilled observer's catch-up trigger is retried before giving up. The whole no-loss guarantee
    /// of the spill rests on the catch-up actually starting, so a transient failure to start it must not strand the
    /// observer; retries make the trigger reliable and a final failure is logged as an error.
    /// </summary>
    const int MaxCatchupTriggerAttempts = 3;

    static readonly TimeSpan _catchupTriggerRetryBaseDelay = TimeSpan.FromMilliseconds(200);

    readonly ITaskFactory _taskFactory;
    readonly IGrainFactory _grainFactory;
    readonly IMeter<AppendedEventsQueue> _meter;
    readonly IActivitySource<AppendedEventsQueue> _activitySource;
    readonly ILogger<AppendedEventsQueue> _logger;
    readonly IShutdownDrainBudget? _stopBudget;
    readonly Channel<IReadOnlyList<AppendedEvent>> _channel;
    readonly int _queueDepletionWaitTimeoutMs;
    readonly AsyncManualResetEvent _queueEmptyEvent = new();
    readonly Lock _subscriptionsLock = new();
    readonly List<AppendedEventsQueueObserverSubscription> _subscriptions = [];
    readonly Dictionary<ObserverKey, AppendedEventsQueueObserverLane> _lanes = [];
    readonly List<Task> _catchupTriggers = [];
    readonly int _laneCapacity;
    readonly SuspectedSkipWarningLimiter _suspectedUndeliveredLimiter = new();
    int _pendingItems;
    Task _queueTask = Task.CompletedTask;
    bool _isDisposed;
    bool _isDraining;
    volatile bool _isResetting;
    IMeterScope<AppendedEventsQueue>? _metrics;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="taskFactory"><see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="meter"><see cref="IMeterScope{T}"/> for metering.</param>
    /// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
    /// <param name="options"><see cref="IOptions{T}"/> for <see cref="ChronicleOptions"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="stopBudget">The shared deadline; optional for standalone specification queues.</param>
    public AppendedEventsQueue(
        ITaskFactory taskFactory,
        IGrainFactory grainFactory,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<AppendedEventsQueue> meter,
        [FromKeyedServices(WellKnown.MeterName)] IActivitySource<AppendedEventsQueue> activitySource,
        IOptions<ChronicleOptions> options,
        ILogger<AppendedEventsQueue> logger,
        IShutdownDrainBudget? stopBudget = null)
    {
        _taskFactory = taskFactory;
        _grainFactory = grainFactory;
        _meter = meter;
        _activitySource = activitySource;
        _logger = logger;
        _stopBudget = stopBudget;

        var eventsConfig = options.Value.Events;
        _queueDepletionWaitTimeoutMs = eventsConfig.QueueDepletionWaitTimeoutMilliseconds;
        var capacity = eventsConfig.QueueBoundedCapacity;
        _laneCapacity = capacity;
        _channel = capacity > 0
            ? Channel.CreateBounded<IReadOnlyList<AppendedEvent>>(new BoundedChannelOptions(capacity)
            {
                // Wait mode is load-bearing for the spill: with the channel full, TryWrite returns false (rather than
                // dropping), which is how Enqueue detects overflow and spills to catch-up instead of blocking.
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            })
            : Channel.CreateUnbounded<IReadOnlyList<AppendedEvent>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        StartQueueHandler();
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var queueId = (int)this.GetPrimaryKeyLong(out var key);
        _eventSequenceKey = EventSequenceKey.Parse(key!);
        _metrics = _meter.BeginScope(key!, queueId);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _isDraining = true;
        _channel.Writer.TryComplete();

        // An ordinary grain deactivation must not start or consume the kernel's future shutdown budget.
        using var standaloneDeadline = _stopBudget?.IsStopping != true
            ? new CancellationTokenSource(_stopBudget?.Timeout ?? TimeSpan.FromSeconds(8))
            : null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, standaloneDeadline?.Token ?? _stopBudget!.Token);
        try
        {
            await _queueTask.WaitAsync(deadline.Token);
            await Task.WhenAll(StopLanes(discard: false)).WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            _logger.DrainUnfinished(_eventSequenceKey, _pendingItems);
        }
        finally
        {
            Dispose();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Enqueue never blocks on observer consumption. When the bounded channel is full the batch is not queued;
    /// instead the queue's subscribed observers are spilled to their catch-up path (see <see cref="SpillToCatchup"/>),
    /// which re-reads the exact missed range from each observer's persisted next-event-sequence-number by cursor.
    /// This deliberately relaxes the previous back-pressure guarantee — an append no longer waits for observers to
    /// drain — so a slow observer can never stall appends. No event is lost: the events are already durable in the
    /// event log before they are enqueued, and the observer's cursor is not advanced past the skipped range.
    /// </remarks>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        if (_isDisposed || _isDraining)
        {
            // Events are durable; a new owner catches up from the observers' persisted cursors.
            return Task.CompletedTask;
        }

        var batch = appendedEvents as AppendedEvent[] ?? appendedEvents.ToArray();
        using var span = _activitySource.Enqueue();
        span?.Activity?.Tag(_eventSequenceKey.EventStore);
        span?.Activity?.Tag(_eventSequenceKey.Namespace);
        span?.Activity?.Tag(_eventSequenceKey.EventSequenceId);

        Interlocked.Increment(ref _pendingItems);
        _queueEmptyEvent.Reset();
        if (_channel.Writer.TryWrite(batch))
        {
            _metrics?.EventsEnqueued(batch.Length);
            return Task.CompletedTask;
        }

        if (Interlocked.Decrement(ref _pendingItems) == 0)
        {
            _queueEmptyEvent.Set();
        }

        SpillSubscribersToCatchup();
        _metrics?.EventsSpilledToCatchup(batch.Length);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ResetForDevelopment()
    {
        _isResetting = true;
        _isDraining = true;
        Task[] catchupTriggers;
        lock (_subscriptionsLock)
        {
            _subscriptions.Clear();
            catchupTriggers = [.. _catchupTriggers];
        }

        _channel.Writer.TryComplete();

        // The worker owns the channel reader. It discards the backlog, but must settle any delivery already in flight
        // before storage can be wiped. A reset failure propagates; a deadline must not turn an unfinished reset green.
        await _queueTask;
        await Task.WhenAll(StopLanes(discard: true));
        await Task.WhenAll(catchupTriggers);
    }

    /// <inheritdoc/>
    public Task SpillToCatchup()
    {
        SpillSubscribersToCatchup();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null)
    {
        lock (_subscriptionsLock)
        {
            if (_isResetting)
            {
                return Task.CompletedTask;
            }

            // Replace any existing subscription for this observer instead of appending a
            // duplicate. Re-subscription is legitimate (the Observing state may be entered
            // multiple times during the observer's lifetime as it cycles through Routing for
            // catch-up); each entry would otherwise leave stale duplicates behind and cause
            // observer.Handle to be invoked multiple times for the same event.
            _subscriptions.RemoveAll(subscription => subscription.ObserverKey == observerKey);
            _subscriptions.Add(new(observerKey, eventTypes.Select(eventType => eventType.Id).ToArray(), filters));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SubscribeToAllEventTypes(ObserverKey observerKey, ObserverFilters? filters = null)
    {
        lock (_subscriptionsLock)
        {
            if (_isResetting)
            {
                return Task.CompletedTask;
            }

            _subscriptions.RemoveAll(subscription => subscription.ObserverKey == observerKey);
            _subscriptions.Add(new(observerKey, [], filters, AllEventTypes: true));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(ObserverKey observerKey)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        lock (_subscriptionsLock)
        {
            _subscriptions.RemoveAll(subscription => subscription.ObserverKey == observerKey);
            CloseLane(observerKey);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AppendedEventsQueueObserverSubscription>> GetSubscriptions()
    {
        lock (_subscriptionsLock)
        {
            return Task.FromResult<IReadOnlyList<AppendedEventsQueueObserverSubscription>>([.. _subscriptions]);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _isDisposed = true;
        _channel.Writer.TryComplete();
        StopLanes(discard: true);

        _metrics?.Dispose();
    }

    /// <summary>
    /// Await the queue to be depleted.
    /// </summary>
    /// <param name="periodNum">Optional amount of times it will check the queue.</param>
    /// <param name="periodDelay">Optional time in ms it will wait after each check.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This method will block until the queue is depleted. This is useful for testing purposes.
    /// It is not exposed on the interface as it is not intended for production use.
    /// </remarks>
    public async Task AwaitQueueDepletion(int periodNum = 10, int periodDelay = 10)
    {
        await Task.Run(async () =>
        {
            if (Debugger.IsAttached)
            {
                while (_pendingItems > 0)
                {
                    await Task.Delay(periodDelay);
                }

                await _queueEmptyEvent.WaitAsync();
            }
            else
            {
                var count = periodNum;
                while (_pendingItems > 0)
                {
                    await Task.Delay(periodDelay);
                    if (--count == 0)
                    {
                        break;
                    }
                }

                await _queueEmptyEvent.WaitAsync().WaitAsync(TimeSpan.FromMilliseconds(_queueDepletionWaitTimeoutMs));
            }
        });
    }

    static bool MatchesFilters(AppendedEventsQueueObserverSubscription subscription, AppendedEvent @event)
    {
        return subscription.Filters?.Matches(@event) ?? true;
    }

    static bool MatchesSubscription(AppendedEventsQueueObserverSubscription subscription, AppendedEvent @event)
    {
        if (!subscription.AllEventTypes && !subscription.EventTypeIds.Contains(@event.Context.EventType.Id))
        {
            return false;
        }

        if (!MatchesFilters(subscription, @event))
        {
            return false;
        }

        if (@event.Context.EventType.Id == GlobalEventTypes.Redaction && !subscription.AllEventTypes)
        {
            return IsRedactionForSubscribedEventType(@event, subscription.EventTypeIds);
        }

        return true;
    }

    static bool IsRedactionForSubscribedEventType(AppendedEvent @event, IEnumerable<EventTypeId> subscribedEventTypeIds)
    {
        if (@event.Content is not IDictionary<string, object?> contentDict || !contentDict.TryGetValue("originalEventType", out var originalEventTypeObj))
        {
            return false;
        }

        var originalEventTypeId = originalEventTypeObj?.ToString();
        return originalEventTypeId is not null && subscribedEventTypeIds.Contains(new EventTypeId(originalEventTypeId));
    }

    /// <summary>
    /// Gets the filtered events for a subscription.
    /// </summary>
    /// <param name="events">The events to filter.</param>
    /// <param name="subscription">The subscription to match events against.</param>
    /// <returns>An array of matching events.</returns>
    static AppendedEvent[] GetFilteredEvents(
        List<AppendedEvent> events,
        AppendedEventsQueueObserverSubscription subscription)
    {
        var matchingEvents = new AppendedEvent[events.Count];
        var numberOfMatchingEvents = 0;
        foreach (var @event in events)
        {
            if (!MatchesSubscription(subscription, @event))
            {
                continue;
            }

            matchingEvents[numberOfMatchingEvents++] = @event;
        }

        if (numberOfMatchingEvents == 0)
        {
            return [];
        }

        return numberOfMatchingEvents == matchingEvents.Length
            ? matchingEvents
            : matchingEvents[..numberOfMatchingEvents];
    }

    AppendedEventsQueueObserverSubscription[] GetSubscriptionsSnapshot()
    {
        lock (_subscriptionsLock)
        {
            return [.. _subscriptions];
        }
    }

    void StartQueueHandler()
    {
        if (_isDisposed)
        {
            return;
        }

        _queueTask = _taskFactory.Run(QueueHandler);
    }

    async Task QueueHandler()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                if (_isDisposed)
                {
                    return;
                }

                _queueEmptyEvent.Reset();

                // Coalesce the queued backlog: drain up to MaxCoalescedBatchesPerDispatch batches and dispatch them as
                // one unit, turning a burst of single-event appends into one Handle call per partition instead of many.
                var coalesced = new List<AppendedEvent>();
                var batchesDrained = 0;
                while (batchesDrained < MaxCoalescedBatchesPerDispatch && _channel.Reader.TryRead(out var events))
                {
                    coalesced.AddRange(events);
                    batchesDrained++;
                }

                if (batchesDrained == 0)
                {
                    continue;
                }

                try
                {
                    if (!_isResetting)
                    {
                        Route(coalesced, batchesDrained);
                        _metrics?.EventsHandled(coalesced.Count);
                    }
                }
                catch (Exception ex)
                {
                    // Log and move on — the observer's own partition-failure and catchup mechanism handles recovery.
                    // Retrying here would cause an unbounded tight loop that exhausts memory.
                    _logger.NotifyingObserversFailed(ex);
                    _metrics?.EventsHandlingFailures();
                }

                if (Interlocked.Add(ref _pendingItems, -batchesDrained) == 0)
                {
                    _queueEmptyEvent.Set();
                }
            }
        }
        catch (Exception exception)
        {
            _logger.QueueHandlerFailed(exception);
        }
    }

    /// <summary>
    /// Routes a coalesced batch to the lane of every observer it holds events for, without waiting for any of them.
    /// </summary>
    /// <param name="events">The coalesced events.</param>
    /// <param name="batches">The number of queued batches the events came from.</param>
    void Route(List<AppendedEvent> events, int batches)
    {
        // A stable sort: events sharing a sequence number keep the order they were enqueued in.
        var sorted = events.OrderBy(@event => @event.Context.SequenceNumber).ToList();
        foreach (var subscription in GetSubscriptionsSnapshot())
        {
            var matching = GetFilteredEvents(sorted, subscription);
            if (matching.Length > 0)
            {
                RouteToLane(subscription.ObserverKey, matching, batches);
            }
        }
    }

    void RouteToLane(ObserverKey observerKey, AppendedEvent[] events, int batches)
    {
        lock (_subscriptionsLock)
        {
            // The subscription may have gone since the snapshot was taken; it then must not get a lane back.
            if (_isResetting || !_subscriptions.Exists(subscription => subscription.ObserverKey == observerKey))
            {
                return;
            }

            if (!_lanes.TryGetValue(observerKey, out var lane) || lane.IsClosed)
            {
                lane = new AppendedEventsQueueObserverLane(observerKey, DeliverToObserver, LaneSettled, LaneFailed, work => _taskFactory.Run(work));
                _lanes[observerKey] = lane;
            }

            if (_laneCapacity > 0 && lane.PendingBatches + batches > _laneCapacity)
            {
                SpillObserverToCatchup(observerKey);
                _metrics?.EventsSpilledToCatchup(events.Length);
                return;
            }

            // Counted before the queue settles the batches it drained, so the queue never looks empty in between.
            Interlocked.Add(ref _pendingItems, batches);
            if (!lane.TryRoute(events, batches))
            {
                LaneSettled(batches);
            }
        }
    }

    /// <summary>
    /// Delivers one observer's events in consecutive same-partition runs, in sequence order. Never across partitions
    /// in parallel: handling a higher-numbered partition first would advance the observer's NextEventSequenceNumber
    /// past lower-numbered events from another partition and drop them.
    /// </summary>
    /// <param name="lane">The <see cref="AppendedEventsQueueObserverLane"/> of the observer.</param>
    /// <param name="events">The events, in sequence order.</param>
    /// <returns>Awaitable task.</returns>
    async Task DeliverToObserver(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events)
    {
        var observerKey = lane.ObserverKey;
        var observer = _grainFactory.GetGrain<IObserver>(observerKey);
        var index = 0;
        while (index < events.Count)
        {
            var partition = events[index].Context.EventSourceId;
            var start = index;
            while (index < events.Count && events[index].Context.EventSourceId == partition)
            {
                index++;
            }

            if (_isResetting || lane.IsClosed)
            {
                return;
            }

            var partitionEvents = new AppendedEvent[index - start];
            for (var i = 0; i < partitionEvents.Length; i++)
            {
                partitionEvents[i] = events[start + i];
            }

            try
            {
                await DispatchWithTracing(observer, partition, partitionEvents, observerKey);
            }
            catch (Exception ex)
            {
                // Contain the failure to this partition so the rest of the delivery still goes through; the
                // observer's own partition-failure and catch-up machinery recovers the failed one.
                LaneFailed(ex);
            }
        }
    }

    void LaneSettled(int batches)
    {
        if (Interlocked.Add(ref _pendingItems, -batches) == 0)
        {
            _queueEmptyEvent.Set();
        }
    }

    void LaneFailed(Exception exception)
    {
        _logger.NotifyingObserversFailed(exception);
        _metrics?.EventsHandlingFailures();
    }

    /// <summary>
    /// Closes an observer's lane, discarding what it has not delivered yet. Must be called holding the subscriptions lock.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
    void CloseLane(ObserverKey observerKey)
    {
        if (_lanes.Remove(observerKey, out var lane))
        {
            lane.Close();
        }
    }

    /// <summary>
    /// Stops every lane.
    /// </summary>
    /// <param name="discard">True to discard what the lanes have not delivered yet; false to let them drain.</param>
    /// <returns>The lanes' completions.</returns>
    Task[] StopLanes(bool discard)
    {
        lock (_subscriptionsLock)
        {
            var lanes = _lanes.Values.ToArray();
            foreach (var lane in lanes)
            {
                if (discard)
                {
                    lane.Close();
                }
                else
                {
                    lane.Complete();
                }
            }

            if (discard)
            {
                _lanes.Clear();
            }

            return [.. lanes.Select(lane => lane.Completion)];
        }
    }

    /// <summary>
    /// Relieves one observer that has fallen too far behind: removes its subscription, discards its lane and drives
    /// it through its catch-up path, which re-reads what it missed from its persisted cursor. The other observers on
    /// the queue are not affected. Must be called holding the subscriptions lock.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
    void SpillObserverToCatchup(ObserverKey observerKey)
    {
        _subscriptions.RemoveAll(subscription => subscription.ObserverKey == observerKey);
        CloseLane(observerKey);
        _logger.SpillingObserverToCatchup(observerKey);
        _catchupTriggers.RemoveAll(task => task.IsCompleted);
        _catchupTriggers.Add(TriggerCatchup(observerKey));
    }

    /// <summary>
    /// Relieves back-pressure without blocking the append: removes the queue's live subscriptions and drives each
    /// affected observer through its catch-up path. Removing the subscriptions guarantees the consumer delivers no
    /// further live batches to those observers, so their next-event-sequence-number cannot advance past the skipped
    /// range; catch-up then recovers from that persisted cursor by re-reading the missed events from the log, and each
    /// observer re-subscribes once it has caught up. Coarse by design — a burst that fills one queue spills every
    /// observer sharing it — but safe: an extra catch-up is idempotent, whereas a dropped live delivery would lose
    /// events silently.
    /// </summary>
    void SpillSubscribersToCatchup()
    {
        ObserverKey[] observerKeys;
        lock (_subscriptionsLock)
        {
            if (_subscriptions.Count == 0)
            {
                return;
            }

            observerKeys = _subscriptions.Select(subscription => subscription.ObserverKey).ToArray();
            _subscriptions.Clear();
            foreach (var observerKey in observerKeys)
            {
                CloseLane(observerKey);
            }
        }

        _logger.SpillingToCatchup(observerKeys.Length);
        foreach (var observerKey in observerKeys)
        {
            lock (_subscriptionsLock)
            {
                if (_isResetting)
                {
                    return;
                }

                _catchupTriggers.RemoveAll(task => task.IsCompleted);
                _catchupTriggers.Add(TriggerCatchup(observerKey));
            }
        }
    }

    /// <summary>
    /// Starts an observer's catch-up out of the append path and does not swallow the outcome. <c language="csharp">CatchUp</c> returns
    /// once the catch-up job has been started (not when it completes), so this waits only for the start — never for
    /// observer processing — and therefore never re-couples appends to observer speed. A failure to start is logged
    /// and retried a bounded number of times so a transient transport/job-subsystem fault does not permanently strand
    /// a spilled observer behind the gap; exhausting the retries is logged as an error. Disposal of the queue while
    /// the retries are in progress abandons them, which is logged as well so no spill ever ends without a trace.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the spilled observer to recover.</param>
    /// <returns>Awaitable task.</returns>
    async Task TriggerCatchup(ObserverKey observerKey)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerKey);
        for (var attempt = 1; attempt <= MaxCatchupTriggerAttempts; attempt++)
        {
            if (_isDisposed || _isResetting)
            {
                _logger.SpillCatchupTriggerAbandonedOnDispose(observerKey);
                return;
            }

            try
            {
                await observer.CatchUp();
                return;
            }
            catch (Exception ex)
            {
                _logger.SpillCatchupTriggerFailed(observerKey, attempt, MaxCatchupTriggerAttempts, ex);
                if (attempt < MaxCatchupTriggerAttempts)
                {
                    await Task.Delay(_catchupTriggerRetryBaseDelay * attempt);
                }
                else
                {
                    _logger.SpillCatchupTriggerAbandoned(observerKey);
                }
            }
        }
    }

    async Task DispatchWithTracing(IObserver observer, EventSourceId partition, AppendedEvent[] events, ObserverKey observerKey)
    {
        using var span = _activitySource.Dispatch();
        span?.Activity?.Tag(observerKey);
        try
        {
            await observer.Handle(partition, events);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            ReportSuspectedUndeliveredEvents(partition, events, observerKey);
            throw;
        }
    }

    /// <summary>
    /// Counts and logs events whose dispatch to an observer failed. The queue cannot tell whether the observer got to
    /// handle them, so they are only suspected to be undelivered. A failure of this telemetry never replaces the
    /// failure being reported.
    /// </summary>
    /// <param name="partition">The partition of the events.</param>
    /// <param name="events">The events whose dispatch failed.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
#pragma warning disable RCS1075 // Telemetry failures are deliberately swallowed: they must never replace the dispatch failure being reported
    void ReportSuspectedUndeliveredEvents(EventSourceId partition, AppendedEvent[] events, ObserverKey observerKey)
    {
        try
        {
            _metrics?.EventsSuspectedUndelivered(events.Length, observerKey.ObserverId);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }

        try
        {
            if (!_suspectedUndeliveredLimiter.TryAdmit(out var suppressed))
            {
                return;
            }

            if (suppressed > 0)
            {
                _logger.SuspectedUndeliveredWarningsSuppressed(suppressed, _suspectedUndeliveredLimiter.WindowSeconds);
            }

            _logger.SuspectedUndeliveredEvents(events.Length, events[0].Context.SequenceNumber, events[^1].Context.SequenceNumber, partition, observerKey);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }
    }
#pragma warning restore RCS1075
}
