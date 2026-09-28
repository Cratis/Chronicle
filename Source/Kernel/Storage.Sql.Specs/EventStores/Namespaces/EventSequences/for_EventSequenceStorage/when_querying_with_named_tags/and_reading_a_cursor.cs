// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_reading_a_cursor : given.a_storage_with_named_tags
{
    AppendedEvent[] _events;

    async Task Because()
    {
        using var cursor = await _storage.GetRange(EventSequenceNumber.First, EventSequenceNumber.Max);
        await cursor.MoveNext();
        _events = cursor.Current.ToArray();
    }

    [Fact] void should_hydrate_all_event_tags() => _events.Select(_ => _.Context.NamedTags.Count()).ShouldEqual([2, 2, 0]);
    [Fact] void should_preserve_values() => _events[0].Context.NamedTags.First().Value.ShouldEqual("a:b");
}
