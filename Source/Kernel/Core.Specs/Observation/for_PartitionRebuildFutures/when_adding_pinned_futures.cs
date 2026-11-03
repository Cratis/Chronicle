// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildFutures;

public class when_adding_pinned_futures : given.partition_rebuild_futures
{
    ProjectionFuture _first;
    ProjectionFuture _second;
    int _afterSecond;
    int _afterDuplicate;
    IEnumerable<ProjectionFuture> _remaining;

    async Task Because()
    {
        _first = Future(1);
        _second = Future(1, 2);
        await _futures.AddPinnedFuture(_first);
        _afterSecond = await _futures.AddPinnedFuture(_second);
        _afterDuplicate = await _futures.AddPinnedFuture(Future(1, 2));
        await _futures.ResolveFuture(_first.Id);
        _remaining = await _futures.GetFutures();
    }

    [Fact] void should_retain_different_generations_of_one_event() => _afterSecond.ShouldEqual(2);
    [Fact] void should_deduplicate_the_same_generation() => _afterDuplicate.ShouldEqual(2);
    [Fact] void should_resolve_only_the_requested_future() => _remaining.ShouldContainOnly(_second);
}
