// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_converting_the_event_source;

public class and_no_event_source_was_stored : Specification
{
    EventEntry _entry;
    EventSourceName _result;

    /// <summary>A null column is every event stored before event sources existed.</summary>
    void Establish() => _entry = new EventEntry { EventSourceId = "aggregate-1", EventSource = null };

    void Because() => _result = EventEntryConverter.ToEventSourceName(_entry);

    [Fact] void should_read_the_event_source_as_not_set() => _result.ShouldEqual(EventSourceName.NotSet);
}
