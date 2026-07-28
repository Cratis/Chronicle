// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionFutures;

public class when_adding_and_resolving_futures : given.the_futures_grain
{
    ProjectionFuture _first;
    ProjectionFuture _second;
    int _pendingAfterFirstAdd;
    int _pendingAfterSecondAdd;
    int _pendingAfterDuplicateAdd;
    IEnumerable<ProjectionFuture> _remaining;

    async Task Because()
    {
        _first = CreateFuture(1);
        _second = CreateFuture(2);
        _pendingAfterFirstAdd = await _grain.AddFuture(_first);
        _pendingAfterSecondAdd = await _grain.AddFuture(_second);
        _pendingAfterDuplicateAdd = await _grain.AddFuture(_first);
        await _grain.ResolveFuture(_first.Id);
        _remaining = await _grain.GetFutures();
    }

    [Fact] void should_report_one_pending_after_first_add() => _pendingAfterFirstAdd.ShouldEqual(1);
    [Fact] void should_report_two_pending_after_second_add() => _pendingAfterSecondAdd.ShouldEqual(2);
    [Fact] void should_not_grow_on_duplicate_add() => _pendingAfterDuplicateAdd.ShouldEqual(2);
    [Fact] void should_keep_only_the_unresolved_future_pending() => _remaining.ShouldContainOnly(_second);
    [Fact] void should_not_accumulate_added_futures() => _stateStorage.State.AddedFutures.ShouldBeEmpty();
    [Fact] void should_not_accumulate_resolved_futures() => _stateStorage.State.ResolvedFutures.ShouldBeEmpty();
}
