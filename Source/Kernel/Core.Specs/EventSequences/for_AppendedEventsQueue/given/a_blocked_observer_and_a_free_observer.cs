// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// Two observers on one queue for the same event type: one whose every delivery waits until it is released, standing
/// in for a reactor calling a slow downstream service, and one that answers straight away.
/// </summary>
public class a_blocked_observer_and_a_free_observer : all_dependencies
{
    protected readonly EventType _eventType = new("Some event", 1);
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly ConcurrentQueue<HandledEvents> _blockedObserverHandled = [];
    protected readonly ConcurrentQueue<HandledEvents> _freeObserverHandled = [];
    protected ObserverKey _blockedObserverKey;
    protected ObserverKey _freeObserverKey;
    protected IObserver _blockedObserver;
    protected IObserver _freeObserver;
    protected AppendedEventsQueue _queue;
    readonly Lock _changedLock = new();
    TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ulong _nextSequenceNumber;

    protected virtual int QueueBoundedCapacity => new Configuration.Events().QueueBoundedCapacity;

    protected int FreeObserverEventCount => _freeObserverHandled.Sum(handled => handled.Events.Count());

    async Task Establish()
    {
        _blockedObserverKey = new ObserverKey("Blocked observer", "Some event store", "Some namespace", "Some event sequence");
        _blockedObserver = Substitute.For<IObserver>();
        _blockedObserver
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(async callInfo =>
            {
                _blockedObserverHandled.Enqueue(new(callInfo.Arg<Key>(), callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));
                Changed();
                await _release.Task;
            });
        _blockedObserver.CatchUp().Returns(_ =>
        {
            Changed();
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(_blockedObserverKey).Returns(_blockedObserver);

        _freeObserverKey = new ObserverKey("Free observer", "Some event store", "Some namespace", "Some event sequence");
        _freeObserver = Substitute.For<IObserver>();
        _freeObserver
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(callInfo =>
            {
                _freeObserverHandled.Enqueue(new(callInfo.Arg<Key>(), callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));
                Changed();
                return Task.CompletedTask;
            });
        _grainFactory.GetGrain<IObserver>(_freeObserverKey).Returns(_freeObserver);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            new ActivitySource<AppendedEventsQueue>(),
            Options.Create(new ChronicleOptions
            {
                Events = new Configuration.Events { QueueBoundedCapacity = QueueBoundedCapacity }
            }),
            Substitute.For<ILogger<AppendedEventsQueue>>());

        await _queue.Subscribe(_blockedObserverKey, [_eventType]);
        await _queue.Subscribe(_freeObserverKey, [_eventType]);
    }

    protected AppendedEvent EventFor(EventSourceId eventSourceId) => AppendedEvent.Empty() with
    {
        Context = EventContext.Empty with
        {
            EventType = _eventType,
            EventSourceId = eventSourceId,
            SequenceNumber = _nextSequenceNumber++
        }
    };

    /// <summary>
    /// Waits until the condition holds, re-checking it each time one of the observers is called. Gives up after a
    /// bound so a spec whose condition never holds fails on its assertions instead of hanging.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task WaitTill(Func<bool> condition)
    {
        var bound = TimeSpan.FromSeconds(5);
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            // Take the signal before checking, so a change between the check and the wait is not missed.
            Task nextChange;
            lock (_changedLock)
            {
                nextChange = _changed.Task;
            }

            var remaining = bound - elapsed.Elapsed;
            if (condition() || remaining <= TimeSpan.Zero)
            {
                return;
            }

            try
            {
                await nextChange.WaitAsync(remaining);
            }
            catch (TimeoutException)
            {
                return;
            }
        }
    }

    void Changed()
    {
        TaskCompletionSource changed;
        lock (_changedLock)
        {
            changed = _changed;
            _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        changed.TrySetResult();
    }
}
