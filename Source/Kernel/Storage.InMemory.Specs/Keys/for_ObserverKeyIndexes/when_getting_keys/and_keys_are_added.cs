// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_keys_are_added : given.an_event_sequence_with_events
{
    async Task Because()
    {
        var index = await _indexes.GetFor(_observerKey, ObserverReaderScope.Wildcard);
        await index.Add((Key)"ghost");
        _keys = await Enumerate(index.GetKeys(EventSequenceNumber.First));
    }

    [Fact] void should_enumerate_only_sources_with_events() => _keys.ShouldEqual([(Key)"behind", (Key)"first", (Key)"other-type"]);
    [Fact] void should_not_enumerate_the_added_key() => _keys.ShouldNotContain((Key)"ghost");
}
