// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Tasks;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
public class AppendedEventsQueue : Grain, IAppendedEventsQueue, IDisposable
{
    readonly IGrainFactory _grainFactory;
    readonly ConcurrentQueue<IEnumerable<AppendedEvent>> _queue = new();
    readonly AsyncManualResetEvent _queueEvent = new();
    readonly TaskCompletionSource _queueTaskCompletionSource = new();
    ConcurrentBag<AppendedEventsQueueObserverSubscription> _subscriptions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    public AppendedEventsQueue(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;

        _ = Task.Run(QueueHandler);
    }

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        _queueEvent.Set();
        _queue.Enqueue(appendedEvents);
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
    }

    async Task QueueHandler()
    {
        while (!_queueTaskCompletionSource.Task.IsCanceled)
        {
            await _queueEvent.WaitAsync();
            if (_queueTaskCompletionSource.Task.IsCanceled)
            {
                return;
            }

            while (_queue.TryDequeue(out var events))
            {
                foreach (var subscription in _subscriptions)
                {
                    var actualEvents = events.Where(@event => subscription.EventTypeIds.Contains(@event.Metadata.Type.Id)).ToList();
                    if (actualEvents.Count == 0)
                    {
                        continue;
                    }
                    var observer = _grainFactory.GetGrain<IObserver>(subscription.ObserverKey);
                    var partition = actualEvents[0].Context.EventSourceId;
                    await observer.Handle(partition, actualEvents);
                }

                _queueEvent.Reset();
            }
        }
    }
}
