// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_results_are_ready_at_the_same_time : given.a_delegate_with_gated_batches
{
    static readonly string[] _partitions = ["order-1", "order-2", "order-3"];
    readonly CountdownEvent _handled = new(_partitions.Length);
    int _publishing;
    int _firstResult;
    bool _overlapped;
    bool _everyHandlerFinishedWhileTheFirstResultWasPublished;

    protected override int MaxConcurrentPartitions => _partitions.Length;

    async Task Because()
    {
        for (var index = 0; index < _partitions.Length; index++)
        {
            Deliver(_partitions[index], (ulong)index + 1, (ulong)index + 101);
        }

        await Task.WhenAll(_partitions.Select((partition, index) => Started(partition, (ulong)index + 1)));
        for (var index = 0; index < _partitions.Length; index++)
        {
            Release(_partitions[index], (ulong)index + 1);
        }

        await Task.WhenAll(_partitions.Select((_, index) => Answered((ulong)index + 101)));
    }

    void Destroy() => _handled.Dispose();

    protected override void OnHandled(string name) => _handled.Signal();

    protected override void OnResult(ReactorResult result)
    {
        if (Interlocked.Increment(ref _publishing) > 1)
        {
            _overlapped = true;
        }

        // The first result is held until every handler has finished, so any result that is not
        // serialized behind it is published while it is still being delivered.
        if (Interlocked.Exchange(ref _firstResult, 1) == 0)
        {
            _everyHandlerFinishedWhileTheFirstResultWasPublished = _handled.Wait(_deadline);
        }

        Interlocked.Decrement(ref _publishing);
    }

    [Fact] void should_let_every_handler_finish_while_a_result_is_being_published() => _everyHandlerFinishedWhileTheFirstResultWasPublished.ShouldBeTrue();
    [Fact] void should_publish_one_result_at_a_time() => _overlapped.ShouldBeFalse();
    [Fact] void should_publish_every_result() => _results.Count.ShouldEqual(_partitions.Length);
}
