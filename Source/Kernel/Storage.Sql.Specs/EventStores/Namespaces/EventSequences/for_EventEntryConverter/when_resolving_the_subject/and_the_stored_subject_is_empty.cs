// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_resolving_the_subject;

public class and_the_stored_subject_is_empty : Specification
{
    const string EventSource = "aggregate-1";

    EventEntry _entry;
    Subject _result;

    void Establish() => _entry = new EventEntry { EventSourceId = EventSource, Subject = string.Empty };

    void Because() => _result = EventEntryConverter.GetSubject(_entry);

    [Fact] void should_fall_back_to_the_event_source_id() => _result.Value.ShouldEqual(EventSource);
}
