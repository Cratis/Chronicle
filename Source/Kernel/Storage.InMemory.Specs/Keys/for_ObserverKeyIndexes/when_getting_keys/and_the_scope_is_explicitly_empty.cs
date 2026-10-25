// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_is_explicitly_empty : given.an_event_sequence_with_events
{
    async Task Because()
    {
        var index = await _indexes.GetFor(_observerKey, ObserverReaderScope.Explicit([]));
        _keys = await Enumerate(index.GetKeys(EventSequenceNumber.First));
    }

    [Fact] void should_not_enumerate_any_keys() => _keys.ShouldBeEmpty();
}
