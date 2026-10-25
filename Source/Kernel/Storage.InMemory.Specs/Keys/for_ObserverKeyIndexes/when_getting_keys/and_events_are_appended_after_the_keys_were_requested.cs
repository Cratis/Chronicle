// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_events_are_appended_after_the_keys_were_requested : given.an_event_sequence_with_events
{
    async Task Because()
    {
        var index = await _indexes.GetFor(_observerKey, ObserverReaderScope.Explicit([_typeA]));
        var keys = index.GetKeys(EventSequenceNumber.First);
        await Append(4, "new-source", _typeA);
        _keys = await Enumerate(keys);
    }

    [Fact] void should_include_the_source_appended_before_enumeration() => _keys.ShouldEqual([(Key)"behind", (Key)"first", (Key)"new-source"]);
}
