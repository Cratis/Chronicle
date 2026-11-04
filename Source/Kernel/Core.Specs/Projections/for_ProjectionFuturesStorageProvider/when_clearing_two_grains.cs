// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionFuturesStorageProvider;

public class when_clearing_two_grains : given.a_futures_storage_provider
{
    async Task Because()
    {
        await _provider.ClearStateAsync("state", _firstId, _first);
        await _provider.ClearStateAsync("state", _secondId, _second);
        AddFuture(_first.State);
    }

    [Fact] void should_allocate_independent_states() => _first.State.ShouldNotBeSame(_second.State);
    [Fact] void should_allocate_independent_futures() => _first.State.Futures.ShouldNotBeSame(_second.State.Futures);
    [Fact] void should_allocate_independent_added_futures() => _first.State.AddedFutures.ShouldNotBeSame(_second.State.AddedFutures);
    [Fact] void should_allocate_independent_resolved_futures() => _first.State.ResolvedFutures.ShouldNotBeSame(_second.State.ResolvedFutures);
    [Fact] void should_not_add_futures_to_the_other_grain() => _second.State.Futures.ShouldBeEmpty();
    [Fact] void should_not_add_pending_futures_to_the_other_grain() => _second.State.AddedFutures.ShouldBeEmpty();
    [Fact] void should_not_resolve_futures_on_the_other_grain() => _second.State.ResolvedFutures.ShouldBeEmpty();
    [Fact] async Task should_remove_the_first_projections_stored_futures() => await _futuresStorage.Received(1).RemoveAllForProjection("first");
    [Fact] async Task should_remove_the_second_projections_stored_futures() => await _futuresStorage.Received(1).RemoveAllForProjection("second");
}
