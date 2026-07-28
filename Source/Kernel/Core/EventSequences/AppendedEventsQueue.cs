// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
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
    readonly Channel<IReadOnlyList<AppendedEvent>> _channel;
    readonly int _queueDepletionWaitTimeoutMs;
    readonly AsyncManualResetEvent _queueEmptyEvent = new();
    readonly Lock _subscriptionsLock = new();
    readonly List<AppendedEventsQueueObserverSubscription> _subscriptions = [];
    int _pendingItems;
    Task _queueTask = Task.CompletedTask;
    bool _isDisposed;
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
    public AppendedEventsQueue(
        ITaskFactory taskFactory,
        IGrainFactory grainFactory,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<AppendedEventsQueue> meter,
        [FromKeyedServices(WellKnown.MeterName)] IActivitySource<AppendedEventsQueue> activitySource,
        IOptions<ChronicleOptions> options,
        ILogger<AppendedEventsQueue> logger)
    {
        _taskFactory = taskFactory;
        _grainFactory = grainFactory;
        _meter = meter;
        _activitySource = activitySource;
        _logger = logger;

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

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var queueId = (int)this.GetPrimaryKeyLong(out var key);
        _eventSequenceKey = EventSequenceKey.Parse(key!);
        _metrics = _meter.BeginScope(key!, queueId);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
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
        _isDisposed = true;
        _channel.Writer.TryComplete();

        if (!_queueTask.IsCompleted)
        {
            try
            {
                _queueTask.Wait(1000);
            }
            catch { }
        }

        if (_queueTask.Status is
                TaskStatus.RanToCompletion or
                TaskStatus.Canceled or
                TaskStatus.Faulted)
        {
            _queueTask.Dispose();
        }

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
        var filters = subscription.Filters;
        if (filters is null)
        {
            return true;
        }

        if (filters.EventSourceType is { } eventSourceType &&
            !eventSourceType.IsDefaultOrUnspecified &&
            @event.Context.EventSourceType != eventSourceType)
        {
            return false;
        }

        if (filters.EventStreamType is { } eventStreamType &&
            !eventStreamType.IsAll &&
            @event.Context.EventStreamType != eventStreamType)
        {
            return false;
        }

        if (filters.Tags.Any() &&
            !filters.Tags.Any(tag => @event.Context.Tags.Any(t => t.Value == tag)))
        {
            return false;
        }

        return true;
    }

    static bool MatchesSubscription(AppendedEventsQueueObserverSubscription subscription, AppendedEvent @event)
    {
        if (!subscription.EventTypeIds.Contains(@event.Context.EventType.Id))
        {
            return false;
        }

        if (!MatchesFilters(subscription, @event))
        {
            return false;
        }

        if (@event.Context.EventType.Id == GlobalEventTypes.Redaction)
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
                    await Dispatch(coalesced);
                    _metrics?.EventsHandled(coalesced.Count);
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

        var index = 0;
        while (index < sorted.Count)
        {
            var partition = sorted[index].Context.EventSourceId;
            var start = index;
            while (index < sorted.Count && sorted[index].Context.EventSourceId == partition)
            {
                index++;
            }

            var partitionEvents = sorted.GetRange(start, index - start);
            var subscriptions = GetSubscriptionsSnapshot();

            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                var actualEvents = GetFilteredEvents(partitionEvents, subscription);
                if (actualEvents.Length == 0)
                {
                    continue;
                }

                var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
                tasks.Add(DispatchWithTracing(observer, partition, actualEvents, subscription.ObserverKey));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Contain the failure to this partition so a coalesced dispatch still delivers the remaining
                // partitions; the observer's own partition-failure and catchup machinery recovers the failed one.
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
            _ = TriggerCatchup(observerKey);
        }
    }

    /// <summary>
    /// Starts an observer's catch-up out of the append path and does not swallow the outcome. <c>CatchUp</c> returns
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
            if (_isDisposed)
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
        await observer.Handle(partition, events);
    }
}
