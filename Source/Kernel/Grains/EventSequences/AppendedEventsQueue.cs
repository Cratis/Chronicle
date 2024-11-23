// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Tasks;
using Cratis.Tasks;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
public class AppendedEventsQueue : Grain, IAppendedEventsQueue, IDisposable
{
    readonly IGrainFactory _grainFactory;
    readonly ConcurrentQueue<IEnumerable<AppendedEvent>> _queue = new();
    readonly AsyncManualResetEvent _queueEvent = new();
    readonly AsyncManualResetEvent _queueEmptyEvent = new();
    readonly TaskCompletionSource _queueTaskCompletionSource = new();
    readonly Task _queueTask;
    ConcurrentBag<AppendedEventsQueueObserverSubscription> _subscriptions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="taskFactory"><see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    public AppendedEventsQueue(ITaskFactory taskFactory, IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;

        _queueTask = taskFactory.Run(QueueHandler);
    }

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        _queue.Enqueue(appendedEvents);
        _queueEvent.Set();
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
        var subscription = _subscriptions.SingleOrDefault(subscription => subscription.ObserverKey == observerKey);
        if (subscription != null)
        {
            _subscriptions = new(_subscriptions.Except([subscription]));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _queueTaskCompletionSource.SetCanceled();
        _queueTask.Dispose();
    }

    /// <summary>
    /// Await the queue to be depleted.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This method will block until the queue is depleted. This is useful for testing purposes.
    /// It is not exposed on the interface as it is not intended for production use.
    /// </remarks>
    public async Task AwaitQueueDepletion()
    {
        await Task.Run(async () =>
        {
            if (Debugger.IsAttached)
            {
                while (!_queue.IsEmpty)
                {
                    await Task.Delay(10);
                }
                await _queueEmptyEvent.WaitAsync();
            }
            else
            {
                var count = 10;
                while (!_queue.IsEmpty)
                {
                    await Task.Delay(10);
                    if (--count == 0)
                    {
                        break;
                    }
                }

                await _queueEmptyEvent.WaitAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
            }
        });
    }

    async Task QueueHandler()
    {
        while (!_queueTaskCompletionSource.Task.IsCanceled)
        {
            await _queueEvent.WaitAsync();
            _queueEmptyEvent.Reset();
            if (_queueTaskCompletionSource.Task.IsCanceled)
            {
                return;
            }

            while (_queue.TryDequeue(out var events))
            {
                try
                {
                    foreach (var group in events.GroupBy(@event => @event.Context.EventSourceId))
                    {
                        var tasks = new List<Task>();
                        foreach (var subscription in _subscriptions)
                        {
                            var actualEvents = events.Where(@event => subscription.EventTypeIds.Contains(@event.Metadata.Type.Id)).ToList();
                            if (actualEvents.Count == 0)
                            {
                                continue;
                            }
                            var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
                            var partition = group.Key;
                            tasks.Add(observer.Handle(partition, group));
                        }

                        await Task.WhenAll(tasks);
                    }
                }
                catch
                {
                    // We ignore any failures, the queue should never fail
                }
            }

            _queueEvent.Reset();
            _queueEmptyEvent.Set();
        }
    }
}
