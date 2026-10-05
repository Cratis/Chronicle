// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy;

public class when_getting_scope_with_no_dimensions : given_an_event_sequence
{
    ConcurrencyScope _scope;
    ConcurrencyScope _withoutDimensions;

    async Task Because()
    {
        _scope = await _strategy.GetScope(ConcurrencyDimensions.None, _eventSourceId, "Items", "stream-1", "Cart");
        _withoutDimensions = await _strategy.GetScope(_eventSourceId, "Items", "stream-1", "Cart");
    }

    [Fact] void should_give_the_same_scope_as_without_event_sources() => _scope.ShouldEqual(_withoutDimensions);
}
