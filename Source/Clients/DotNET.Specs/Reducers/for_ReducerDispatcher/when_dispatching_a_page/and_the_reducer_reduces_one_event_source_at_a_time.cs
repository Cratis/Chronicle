// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching_a_page;

public class and_the_reducer_reduces_one_event_source_at_a_time : given.a_reducer_dispatcher
{
    int _running;
    int _mostRunning;

    void Establish()
    {
        _handler.ReducerType.Returns(typeof(SequentialReducer));
        _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
            .Returns(async _ =>
            {
                var running = Interlocked.Increment(ref _running);
                InterlockedMax(ref _mostRunning, running);
                // Slow on purpose, not a wait: each reduce stays in flight long enough for an overlapping one to be counted.
                await Task.Delay(20);
                Interlocked.Decrement(ref _running);
                return new ReduceResult(new State("reduced"), 1UL, [], string.Empty);
            });
    }

    async Task Because() => await _dispatcher.Dispatch(
        _handler,
        PageWith(("first", null, [1]), ("second", null, [2]), ("third", null, [3]), ("fourth", null, [4])),
        "ns",
        Publish);

    [Fact] void should_reduce_every_event_source() => _published[0].Partitions.Count.ShouldEqual(4);
    [Fact] void should_never_reduce_two_event_sources_at_once() => _mostRunning.ShouldEqual(1);

    static void InterlockedMax(ref int target, int value)
    {
        var current = target;
        while (value > current)
        {
            var previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }

    [BatchSize(MaxConcurrency = 1)]
    public class SequentialReducer;
}
