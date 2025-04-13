// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Tasks;
using Cratis.Metrics;
using Cratis.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences;

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
    readonly ConcurrentQueue<IEnumerable<AppendedEvent>> _queue = new();
    readonly AsyncManualResetEvent _queueEvent = new();
    readonly AsyncManualResetEvent _queueEmptyEvent = new();
    Task _queueTask = Task.CompletedTask;
    bool _isDisposed;
    ConcurrentBag<AppendedEventsQueueObserverSubscription> _subscriptions = [];
    IMeterScope<AppendedEventsQueue>? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="taskFactory"><see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="meter"><see cref="IMeterScope{T}"/> for metering.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public AppendedEventsQueue(
        ITaskFactory taskFactory,
        IGrainFactory grainFactory,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<AppendedEventsQueue> meter,
        ILogger<AppendedEventsQueue> logger)
    {
        _taskFactory = taskFactory;
        _grainFactory = grainFactory;
        _meter = meter;
        _logger = logger;
        StartQueueHandler();
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var queueId = (int)this.GetPrimaryKeyLong(out var key);
        _metrics = _meter.BeginScope(key, queueId);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        _queue.Enqueue(appendedEvents);
        _queueEvent.Set();
        _metrics?.EventsEnqueued(appendedEvents.Count());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes)
    {
        _subscriptions.Add(new(observerKey, eventTypes.Select(eventType => eventType.Id).ToArray()));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(ObserverKey observerKey)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var subscription = _subscriptions.SingleOrDefault(subscription => subscription.ObserverKey == observerKey);
        if (subscription != null)
        {
            _subscriptions = [.. _subscriptions.Except([subscription])];
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _isDisposed = true;

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
                while (!_queue.IsEmpty)
                {
                    await Task.Delay(periodDelay);
                }
                await _queueEmptyEvent.WaitAsync();
            }
            else
            {
                var count = periodNum;
                while (!_queue.IsEmpty)
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
        while (!_isDisposed)
        {
            try
            {
                await _queueEvent.WaitAsync();
                _queueEmptyEvent.Reset();
                if (_isDisposed)
                {
                    return;
                }

                while (_queue.TryDequeue(out var events))
                {
                    var eventsNotHandled = true;
                    while (eventsNotHandled)
                    {
                        try
                        {
                            var count = events.Count();
                            Func<IEnumerable<AppendedEvent>, Task> handler = count == 1 ?
                                HandleSingle :
                                HandlePartitioned;

                            await handler(events);

                            _metrics?.EventsHandled(count);
                            eventsNotHandled = false;
                        }
                        catch (Exception ex)
                        {
                            // We ignore any failures, the queue should never fail
                            _logger.NotifyingObserversFailed(ex);
                            _metrics?.EventsHandlingFailures();
                        }
                    }
                }

                _queueEvent.Reset();
                _queueEmptyEvent.Set();
            }
            catch (Exception exception)
            {
                _logger.QueueHandlerFailed(exception);
            }
        }
        StartQueueHandler();
    }

    async Task HandleSingle(IEnumerable<AppendedEvent> events)
    {
        var @event = events.First();
        foreach (var subscription in _subscriptions)
        {
            if (!subscription.EventTypeIds.Contains(@event.Metadata.Type.Id))
            {
                continue;
            }
            var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
            var eventToHandle = new List<AppendedEvent> { @event };
            await observer.Handle(@event.Context.EventSourceId, eventToHandle);
        }
    }

    async Task HandlePartitioned(IEnumerable<AppendedEvent> events)
    {
        foreach (var group in events.GroupBy(@event => @event.Context.EventSourceId))
        {
            var tasks = new List<Task>();
            foreach (var subscription in _subscriptions)
            {
                var actualEvents = group.Where(@event => subscription.EventTypeIds.Contains(@event.Metadata.Type.Id)).ToList();
                if (actualEvents.Count == 0)
                {
                    continue;
                }
                var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
                var partition = group.Key;
                tasks.Add(observer.Handle(partition, actualEvents));
            }

            await Task.WhenAll(tasks);
        }
    }
}
