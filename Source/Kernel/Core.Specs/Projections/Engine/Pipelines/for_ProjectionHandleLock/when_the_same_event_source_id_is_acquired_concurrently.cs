// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionHandleLock;

public class when_the_same_event_source_id_is_acquired_concurrently : Specification
{
    const int Contenders = 64;

    ProjectionHandleLock _lock;
    EventSourceId _eventSourceId;
    int _current;
    int _maxObserved;
    int _completed;

    void Establish()
    {
        _lock = new ProjectionHandleLock();
        _eventSourceId = "the-same-partition";
    }

    async Task Because()
    {
        var contenders = Enumerable.Range(0, Contenders).Select(async _ =>
        {
            using var handle = await _lock.AcquireFor(_eventSourceId);
            var observed = Interlocked.Increment(ref _current);
            RecordMax(ref _maxObserved, observed);
            await Task.Delay(1);
            Interlocked.Decrement(ref _current);
            Interlocked.Increment(ref _completed);
        });

        await Task.WhenAll(contenders);
    }

    [Fact]
    void should_let_every_contender_run() => _completed.ShouldEqual(Contenders);

    [Fact]
    void should_never_let_two_contenders_hold_the_same_key_at_once() => _maxObserved.ShouldEqual(1);

    static void RecordMax(ref int max, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref max);
            if (value <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref max, value, current) != current);
    }
}
