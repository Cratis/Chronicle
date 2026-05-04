// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
[KeepAlive]
public class AppendedEventsQueue : Grain, IAppendedEventsQueue, IDisposable
{
    readonly ITaskFactory _taskFactory;
    readonly IGrainFactory _grainFactory;
    readonly IMeter<AppendedEventsQueue> _meter;
    readonly ILogger<AppendedEventsQueue> _logger;
    readonly Channel<IReadOnlyList<AppendedEvent>> _channel;
    readonly AsyncManualResetEvent _queueEmptyEvent = new();
    readonly Lock _subscriptionsLock = new();
    readonly List<AppendedEventsQueueObserverSubscription> _subscriptions = [];
    readonly int _maxConcurrentObserverDispatches;
    int _pendingItems;
    Task _queueTask = Task.CompletedTask;
    bool _isDisposed;
    IMeterScope<AppendedEventsQueue>? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="taskFactory"><see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="meter"><see cref="IMeterScope{T}"/> for metering.</param>
    /// <param name="options"><see cref="IOptions{T}"/> for <see cref="ChronicleOptions"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public AppendedEventsQueue(
        ITaskFactory taskFactory,
        IGrainFactory grainFactory,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<AppendedEventsQueue> meter,
        IOptions<ChronicleOptions> options,
        ILogger<AppendedEventsQueue> logger)
    {
        _taskFactory = taskFactory;
        _grainFactory = grainFactory;
        _meter = meter;
        _logger = logger;

        var eventsConfig = options.Value.Events;
        _maxConcurrentObserverDispatches = eventsConfig.MaxConcurrentObserverDispatches;

        var capacity = eventsConfig.QueueBoundedCapacity;
        _channel = capacity > 0
            ? Channel.CreateBounded<IReadOnlyList<AppendedEvent>>(new BoundedChannelOptions(capacity)
            {
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
    public async Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        var batch = appendedEvents as IReadOnlyList<AppendedEvent> ?? appendedEvents.ToList();
        Interlocked.Increment(ref _pendingItems);
        _queueEmptyEvent.Reset();
        await _channel.Writer.WriteAsync(batch);
        _metrics?.EventsEnqueued(batch.Count);
    }

    /// <inheritdoc/>
    public Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null)
    {
        lock (_subscriptionsLock)
        {
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

                await _queueEmptyEvent.WaitAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
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
            await foreach (var events in _channel.Reader.ReadAllAsync())
            {
                if (_isDisposed)
                {
                    return;
                }

                _queueEmptyEvent.Reset();
                try
                {
                    var count = events.Count;
                    Func<IReadOnlyList<AppendedEvent>, Task> handler = count == 1
                        ? HandleSingle
                        : HandlePartitioned;

                    await handler(events);

                    _metrics?.EventsHandled(count);
                }
                catch (Exception ex)
                {
                    // Log and move on — the observer's own partition-failure and
                    // catchup mechanism handles recovery. Retrying here would cause
                    // an unbounded tight loop that exhausts memory.
                    _logger.NotifyingObserversFailed(ex);
                    _metrics?.EventsHandlingFailures();
                }

                if (Interlocked.Decrement(ref _pendingItems) == 0)
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

    async Task HandleSingle(IReadOnlyList<AppendedEvent> events)
    {
        var @event = events[0];
        foreach (var subscription in GetSubscriptionsSnapshot())
        {
            if (!subscription.EventTypeIds.Contains(@event.Context.EventType.Id))
            {
                continue;
            }

            if (!MatchesFilters(subscription, @event))
            {
                continue;
            }

            var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
            var eventToHandle = new List<AppendedEvent> { @event };
            await observer.Handle(@event.Context.EventSourceId, eventToHandle);
        }
    }

    async Task HandlePartitioned(IReadOnlyList<AppendedEvent> events)
    {
        // Sort events by sequence number and deliver consecutive same-partition batches.
        // This prevents a race condition where processing one partition's events first
        // advances the observer's NextEventSequenceNumber past lower-numbered events
        // from other partitions, causing those events to be incorrectly skipped.
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

            await Parallel.ForEachAsync(
                subscriptions,
                new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrentObserverDispatches },
                async (subscription, ct) =>
                {
                    var actualEvents = partitionEvents
                        .Where(@event => subscription.EventTypeIds.Contains(@event.Context.EventType.Id) && MatchesFilters(subscription, @event))
                        .ToList();
                    if (actualEvents.Count == 0)
                    {
                        return;
                    }

                    var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
                    await observer.Handle(partition, actualEvents);
                });
        }
    }
}
