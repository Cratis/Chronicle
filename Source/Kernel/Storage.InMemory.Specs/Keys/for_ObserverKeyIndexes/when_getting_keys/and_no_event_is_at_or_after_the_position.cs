// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_no_event_is_at_or_after_the_position : given.an_event_sequence_with_events
{
    async Task Because()
    {
        var index = await _indexes.GetFor(_observerKey, ObserverReaderScope.Wildcard);
        _keys = await Enumerate(index.GetKeys(4UL));
    }

    [Fact] void should_not_enumerate_any_keys() => _keys.ShouldBeEmpty();
}
