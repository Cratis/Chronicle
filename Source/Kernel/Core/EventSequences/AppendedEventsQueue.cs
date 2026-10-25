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
    /// The most sequential per-partition deliveries one coalesced dispatch may cost a live observer before it is moved to its catch-up path.
    /// </summary>
    /// <remarks>
    /// Live delivery makes one sequential <c language="csharp">Handle</c> call per run of consecutive same-partition events, each of which persists
    /// observer state. Past this many runs, catch-up (parallel per partition, page-based) is cheaper and gives the same no-loss
    /// guarantee. Do not lower it without benchmark evidence.
    /// </remarks>
    internal const int MaxPartitionRunsPerLiveDispatch = 256;

    /// <summary>
    /// Upper bound on how many queued batches a single dispatch coalesces before delivering. Bounds the size of the
    /// merged batch (and therefore the work of one dispatch); any remaining backlog is drained on the next loop pass.
    /// </summary>
    const int MaxCoalescedBatchesPerDispatch = 100;

    static readonly TimeSpan _catchupTriggerRetryBaseDelay = TimeSpan.FromMilliseconds(200);
    static readonly TimeSpan _catchupTriggerRetryMaxDelay = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _catchupTriggerWarningBudget = TimeSpan.FromMinutes(2);

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
    readonly List<Task> _catchupTriggers = [];
    readonly Dictionary<ObserverKey, CatchupTrigger> _catchupTriggersByObserver = [];
    readonly CancellationTokenSource _catchupCancellation = new();
    readonly CancellationToken _catchupCancellationToken;
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
        _catchupCancellationToken = _catchupCancellation.Token;

        var eventsConfig = options.Value.Events;
        _queueDepletionWaitTimeoutMs = eventsConfig.QueueDepletionWaitTimeoutMilliseconds;
        var capacity = eventsConfig.QueueBoundedCapacity;
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

    /// <summary>
    /// Gets or sets the clock used for catch-up trigger deadlines and retry delays.
    /// </summary>
    internal TimeProvider CatchupTimeProvider { get; set; } = TimeProvider.System;

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
        await _catchupCancellation.CancelAsync();
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
        lock (_subscriptionsLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _catchupCancellation.Cancel();
            _catchupCancellation.Dispose();
        }
        _channel.Writer.TryComplete();

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

                await _queueEmptyEvent.WaitAsync().WaitAsync(TimeSpan.FromMilliseconds(_queueDepletionWaitTimeoutMs), TimeProvider.System);
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

#pragma warning disable RCS1075 // Telemetry failures are deliberately swallowed: they must never replace the spill or delivery they observe
    static void Observe(Action telemetry)
    {
        try
        {
            telemetry();
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }
    }
#pragma warning restore RCS1075

    static List<(EventSourceId Partition, List<AppendedEvent> Events)> SplitIntoPartitionRuns(List<AppendedEvent> sorted)
    {
        var runs = new List<(EventSourceId Partition, List<AppendedEvent> Events)>();
        var index = 0;
        while (index < sorted.Count)
        {
            var partition = sorted[index].Context.EventSourceId;
            var start = index;
            while (index < sorted.Count && sorted[index].Context.EventSourceId == partition)
            {
                index++;
            }

            runs.Add((partition, sorted.GetRange(start, index - start)));
        }

        return runs;
    }

    static int CountRunsUpTo(
        AppendedEventsQueueObserverSubscription subscription,
        List<(EventSourceId Partition, List<AppendedEvent> Events)> runs,
        int limit)
    {
        var count = 0;
        foreach (var (_, runEvents) in runs)
        {
            foreach (var @event in runEvents)
            {
                if (!MatchesSubscription(subscription, @event))
                {
                    continue;
                }

                if (++count >= limit)
                {
                    return count;
                }

                break;
            }
        }

        return count;
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

        _queueTask = DetachedExecution.Start(() => _taskFactory.Run(QueueHandler));
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
                        await Dispatch(coalesced);
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

    async Task Dispatch(IReadOnlyList<AppendedEvent> events)
    {
        // Sort events by sequence number and deliver consecutive same-partition batches. Parallelism is across
        // observers within a partition, never across partitions: handling a higher-numbered partition first would
        // advance an observer's NextEventSequenceNumber past lower-numbered events from another partition and drop them.
        var sorted = events.OrderBy(e => e.Context.SequenceNumber).ToList();
        var runs = SplitIntoPartitionRuns(sorted);

        // Spill decided before any delivery: an observer whose share of this dispatch would cost too many sequential Handle
        // calls is moved to catch-up before anything is handed to it, so its cursor can never pass an event it skipped.
        // Every event here was durable before Enqueue, hence before the spill and the CatchUp() call that follows it, so
        // catch-up covers them all. Skipping it for the rest of the dispatch, including a replacement subscription that
        // appears mid-dispatch, is safe for that reason (delivering would also be harmless: Handle discards anything below
        // the cursor).
        var spilled = SpillFragmentedSubscriptions(runs);

        foreach (var (partition, partitionEvents) in runs)
        {
            var subscriptions = GetSubscriptionsSnapshot();

            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                if (spilled.Contains(subscription.ObserverKey))
                {
                    continue;
                }

                var actualEvents = GetFilteredEvents(partitionEvents, subscription);
                if (actualEvents.Length == 0)
                {
                    continue;
                }

                tasks.Add(DispatchWithTracing(partition, actualEvents, subscription));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Only the failed subscription is spilled: other observers keep receiving the remaining partitions.
                // The spilled observer must catch up before any later live batch can advance its cursor past the hole.
                _logger.NotifyingObserversFailed(ex);
                _metrics?.EventsHandlingFailures();
            }
        }
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
        }

        _logger.SpillingToCatchup(observerKeys.Length);
        foreach (var observerKey in observerKeys)
        {
            StartCatchupTrigger(observerKey);
            ReportSpill(observerKey, AppendedEventsQueueSpillReasons.QueueFull);
        }
    }

    HashSet<ObserverKey> SpillFragmentedSubscriptions(List<(EventSourceId Partition, List<AppendedEvent> Events)> runs)
    {
        var spilled = new HashSet<ObserverKey>();

        // A subscription cannot have more runs than the batch has, so most dispatches skip the scan entirely.
        if (runs.Count <= MaxPartitionRunsPerLiveDispatch)
        {
            return spilled;
        }

        foreach (var subscription in GetSubscriptionsSnapshot())
        {
            if (CountRunsUpTo(subscription, runs, MaxPartitionRunsPerLiveDispatch + 1) > MaxPartitionRunsPerLiveDispatch &&
                SpillFragmentedSubscriptionToCatchup(subscription))
            {
                spilled.Add(subscription.ObserverKey);
            }
        }

        return spilled;
    }

    bool SpillFragmentedSubscriptionToCatchup(AppendedEventsQueueObserverSubscription subscription)
    {
        lock (_subscriptionsLock)
        {
            if (_isDisposed || _isResetting)
            {
                return false;
            }

            // Remove by identity, as for a failed dispatch: a replacement subscription for the same observer must survive,
            // and then the caller must not skip it. A skip implies this subscription was removed and a catch-up trigger started.
            if (_subscriptions.RemoveAll(candidate => ReferenceEquals(candidate, subscription)) == 0)
            {
                return false;
            }

            StartCatchupTrigger(subscription.ObserverKey);
        }

        Observe(() => _logger.SpillingFragmentedObserverToCatchup(subscription.ObserverKey, MaxPartitionRunsPerLiveDispatch));
        ReportSpill(subscription.ObserverKey, AppendedEventsQueueSpillReasons.FragmentedBacklog);
        return true;
    }

    void SpillFailedSubscriptionToCatchup(AppendedEventsQueueObserverSubscription subscription, AppendedEvent[] events)
    {
        lock (_subscriptionsLock)
        {
            // A reactivation may already have replaced this snapshot entry with an equal record. Remove by identity,
            // never by ObserverKey or record equality, so the new subscription is not accidentally dropped.
            if (_subscriptions.RemoveAll(candidate => ReferenceEquals(candidate, subscription)) == 0)
            {
                return;
            }

            StartCatchupTrigger(subscription.ObserverKey);
        }

        _logger.DispatchFailedSpillingToCatchup(subscription.ObserverKey, events[0].Context.SequenceNumber, events[^1].Context.SequenceNumber);
        ReportSpill(subscription.ObserverKey, AppendedEventsQueueSpillReasons.DispatchFailed);
    }

    void StartCatchupTrigger(ObserverKey observerKey)
    {
        lock (_subscriptionsLock)
        {
            if (_isDisposed || _isResetting)
            {
                return;
            }

            // A trigger is already live for this observer: its CatchUp() call may have begun before this spill, so owe it
            // one more call instead of dropping the spill. Owed calls coalesce into a single rerun.
            if (_catchupTriggersByObserver.TryGetValue(observerKey, out var existing))
            {
                existing.Spills++;
                return;
            }

            var trigger = new CatchupTrigger();
            _catchupTriggersByObserver[observerKey] = trigger;
            _catchupTriggers.RemoveAll(task => task.IsCompleted);
            _catchupTriggers.Add(TriggerCatchup(observerKey, trigger));
        }
    }

    /// <summary>
    /// Starts catch-up without waiting for historical processing. Failures back off from 200 ms to five seconds;
    /// after two minutes an unthrottled warning and counter mark the prolonged outage, but retries never give up.
    /// Disposal/reset ends the trigger. If the silo dies, the observer's unchanged persisted cursor and the
    /// missed-event check on its next subscription provide durable recovery. There is at most one live trigger, and so one
    /// <c language="csharp">CatchUp()</c> call, per observer. A spill that lands while it is in flight re-runs it once, after the current call
    /// succeeds, so every spill is followed by a call that started after it.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the spilled observer to recover.</param>
    /// <param name="trigger">The <see cref="CatchupTrigger"/> registered for the observer; this task owns it.</param>
    /// <returns>Awaitable task.</returns>
    async Task TriggerCatchup(ObserverKey observerKey, CatchupTrigger trigger)
    {
        try
        {
            while (true)
            {
                var observedSpills = await RetryCatchupUntilStarted(observerKey, trigger);

                // Check and release in one critical section: a spill either sees the entry (and bumps it, which this
                // check then sees) or finds it gone and starts a new trigger.
                lock (_subscriptionsLock)
                {
                    if (trigger.Spills == observedSpills)
                    {
                        ReleaseCatchupTrigger(observerKey, trigger);
                        return;
                    }
                }

                Observe(() => _logger.CatchupRequestedAgain(observerKey));
            }
        }
        catch (OperationCanceledException) when (_catchupCancellationToken.IsCancellationRequested)
        {
            _logger.SpillCatchupTriggerAbandonedOnDispose(observerKey);
        }
        finally
        {
            lock (_subscriptionsLock)
            {
                ReleaseCatchupTrigger(observerKey, trigger);
            }
        }
    }

    void ReleaseCatchupTrigger(ObserverKey observerKey, CatchupTrigger trigger)
    {
        // Only this task's own entry: a newer trigger registered after this one released must survive.
        if (_catchupTriggersByObserver.TryGetValue(observerKey, out var current) && ReferenceEquals(current, trigger))
        {
            _catchupTriggersByObserver.Remove(observerKey);
        }
    }

    async Task<int> RetryCatchupUntilStarted(ObserverKey observerKey, CatchupTrigger trigger)
    {
        var clock = CatchupTimeProvider;
        var started = clock.GetTimestamp();
        var delay = _catchupTriggerRetryBaseDelay;
        var warned = false;
        for (var attempt = 1; ; attempt++)
        {
            _catchupCancellationToken.ThrowIfCancellationRequested();

            // Snapshot before the call: only spills that land after this point are not covered by it.
            int observedSpills;
            lock (_subscriptionsLock)
            {
                observedSpills = trigger.Spills;
            }

            try
            {
                var observer = _grainFactory.GetGrain<IObserver>(observerKey);
                await observer.CatchUp();
                return observedSpills;
            }
            catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
            {
                var exceededBudget = !warned && clock.GetElapsedTime(started) >= _catchupTriggerWarningBudget;
                ReportCatchupTriggerFailure(observerKey, attempt, ex, exceededBudget);
                warned |= exceededBudget;
            }

            await Task.Delay(delay, clock, _catchupCancellationToken);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _catchupTriggerRetryMaxDelay.Ticks));
        }
    }

#pragma warning disable RCS1075 // Telemetry must not strand a spilled observer by stopping retries
    void ReportSpill(ObserverKey observerKey, string reason) =>
        Observe(() => _metrics?.ObserverSpilledToCatchup(observerKey.ObserverId, reason));

    void ReportCatchupTriggerFailure(ObserverKey observerKey, int attempt, Exception exception, bool exceededBudget)
    {
        try
        {
            if (exceededBudget)
            {
                _logger.SpillCatchupTriggerBudgetExceeded(observerKey);
            }
            _logger.SpillCatchupTriggerFailed(observerKey, attempt, exception);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }

        try
        {
            if (exceededBudget)
            {
                _metrics?.CatchupTriggerBudgetExceeded();
            }
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }
    }
#pragma warning restore RCS1075

    async Task DispatchWithTracing(EventSourceId partition, AppendedEvent[] events, AppendedEventsQueueObserverSubscription subscription)
    {
        using var span = _activitySource.Dispatch();
        span?.Activity?.Tag(subscription.ObserverKey);
        try
        {
            var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
            await observer.Handle(partition, events);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            ReportSuspectedUndeliveredEvents(partition, events, subscription.ObserverKey);
            SpillFailedSubscriptionToCatchup(subscription, events);
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

    /// <summary>
    /// The live catch-up trigger of one spilled observer. Only read and written under <c language="csharp">_subscriptionsLock</c>.
    /// </summary>
    sealed class CatchupTrigger
    {
        /// <summary>
        /// Gets or sets how many spills joined this trigger while it was live.
        /// </summary>
        public int Spills { get; set; }
    }
}
