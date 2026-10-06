// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_converting_the_event_source;

public class and_an_event_source_was_stored : Specification
{
    EventEntry _entry;
    EventSourceName _result;

    void Establish() => _entry = new EventEntry { EventSourceId = "aggregate-1", EventSource = "ShoppingCart" };

    void Because() => _result = EventEntryConverter.ToEventSourceName(_entry);

    [Fact] void should_read_the_stored_event_source() => _result.Value.ShouldEqual("ShoppingCart");
}
