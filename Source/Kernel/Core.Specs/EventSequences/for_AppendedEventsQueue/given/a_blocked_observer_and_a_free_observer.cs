// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
                await _release.Task;
            });
        _blockedObserver.CatchUp().Returns(Task.CompletedTask);
        _grainFactory.GetGrain<IObserver>(_blockedObserverKey).Returns(_blockedObserver);

        _freeObserverKey = new ObserverKey("Free observer", "Some event store", "Some namespace", "Some event sequence");
        _freeObserver = Substitute.For<IObserver>();
        _freeObserver
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(callInfo =>
            {
                _freeObserverHandled.Enqueue(new(callInfo.Arg<Key>(), callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));
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

    protected async Task WaitTill(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition() && !timeout.IsCancellationRequested)
        {
            await Task.Delay(10, CancellationToken.None);
        }
    }
}
