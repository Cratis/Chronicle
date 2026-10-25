// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_has_another_generation : given.an_event_sequence_with_events
{
    async Task Because()
    {
        var index = await _indexes.GetFor(_observerKey, ObserverReaderScope.Explicit([new EventType(_typeA.Id, 2)]));
        _keys = await Enumerate(index.GetKeys(EventSequenceNumber.First));
    }

    [Fact] void should_match_event_type_ids_regardless_of_generation() => _keys.ShouldEqual([(Key)"behind", (Key)"first"]);
}
