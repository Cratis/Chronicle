// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionFuturesStorageProvider;

public class when_clearing_a_grain_again : given.a_futures_storage_provider
{
    ProjectionFuturesState _previous;

    async Task Establish()
    {
        await _provider.ClearStateAsync("state", _firstId, _first);
        _previous = _first.State;
        AddFuture(_previous);
    }

    Task Because() => _provider.ClearStateAsync("state", _firstId, _first);

    [Fact] void should_replace_the_previous_state() => _first.State.ShouldNotBeSame(_previous);
    [Fact] void should_clear_futures() => _first.State.Futures.ShouldBeEmpty();
    [Fact] void should_clear_added_futures() => _first.State.AddedFutures.ShouldBeEmpty();
    [Fact] void should_clear_resolved_futures() => _first.State.ResolvedFutures.ShouldBeEmpty();
    [Fact] void should_preserve_previously_returned_futures() => _previous.Futures.ShouldContainOnly(_future);
    [Fact] void should_preserve_previously_returned_added_futures() => _previous.AddedFutures.ShouldContainOnly(_future);
    [Fact] void should_preserve_previously_returned_resolved_futures() => _previous.ResolvedFutures.ShouldContainOnly(_future);
}
