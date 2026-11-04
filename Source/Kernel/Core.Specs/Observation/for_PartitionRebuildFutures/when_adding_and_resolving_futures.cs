// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildFutures;

public class when_adding_and_resolving_futures : given.partition_rebuild_futures
{
    ProjectionFuture _first;
    ProjectionFuture _second;
    int _afterFirst;
    int _afterSecond;
    int _afterDuplicate;
    IEnumerable<ProjectionFuture> _stored;
    IEnumerable<ProjectionFuture> _remaining;
    IEnumerable<ProjectionFuture> _otherPipelineFutures;

    async Task Because()
    {
        _first = Future(1);
        _second = Future(2);
        _afterFirst = await _futures.AddFuture(_first);
        _afterSecond = await _futures.AddFuture(_second);
        _afterDuplicate = await _futures.AddFuture(Future(1, 2));
        _stored = await _futures.GetFutures();
        await _futures.ResolveFuture(_first.Id);
        await _futures.ResolveFuture(new ProjectionFutureId(Guid.NewGuid()));
        _remaining = await _futures.GetFutures();
        _otherPipelineFutures = await new PartitionRebuildFutures().GetFutures();
    }

    [Fact] void should_report_one_after_first_add() => _afterFirst.ShouldEqual(1);
    [Fact] void should_report_two_after_second_add() => _afterSecond.ShouldEqual(2);
    [Fact] void should_deduplicate_compatibility_futures_by_sequence_number() => _afterDuplicate.ShouldEqual(2);
    [Fact] void should_return_the_stored_futures() => _stored.ShouldContainOnly(_first, _second);
    [Fact] void should_keep_only_the_unresolved_future() => _remaining.ShouldContainOnly(_second);
    [Fact] void should_not_share_futures_between_pipelines() => _otherPipelineFutures.ShouldBeEmpty();
}
