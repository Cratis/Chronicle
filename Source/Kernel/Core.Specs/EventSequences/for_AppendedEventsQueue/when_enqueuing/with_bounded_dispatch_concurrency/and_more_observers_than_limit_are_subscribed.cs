// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_bounded_dispatch_concurrency;

public class and_more_observers_than_limit_are_subscribed : given.a_queue_with_concurrency_limit
{
    const int ObserverCount = 10;
    const int LimitedConcurrency = 3;

    readonly EventType _eventType = new("test-event", 1);
    int _peakConcurrency;
    int _currentConcurrency;
    int _totalHandled;

    protected override int MaxConcurrentDispatches => LimitedConcurrency;

    async Task Establish()
    {
        for (var i = 0; i < ObserverCount; i++)
        {
            var key = new ObserverKey($"observer-{i}", "store", "ns", "seq");
            var observer = Substitute.For<IObserver>();
            var localKey = key;

            observer
                .When(o => o.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
                .Do(_ =>
                {
                    var current = Interlocked.Increment(ref _currentConcurrency);

                    int prev;
                    do
                    {
                        prev = _peakConcurrency;
                        if (current <= prev)
                        {
                            break;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _peakConcurrency, current, prev) != prev);

                    Interlocked.Decrement(ref _currentConcurrency);
                    Interlocked.Increment(ref _totalHandled);
                });

            observer
                .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
                .Returns(Task.CompletedTask);

            _grainFactory.GetGrain<IObserver>(localKey).Returns(observer);
            await _queue.Subscribe(localKey, [_eventType]);
        }
    }

    async Task Because()
    {
        var eventSourceId = new EventSourceId("partition-1");
        var @event = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = eventSourceId
            }
        };

        await _queue.Enqueue([@event]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_have_dispatched_to_all_observers() => _totalHandled.ShouldEqual(ObserverCount);
    [Fact] void should_never_exceed_the_concurrency_limit() => (_peakConcurrency <= LimitedConcurrency).ShouldBeTrue();
}
