// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_resolving_the_subject;

public class and_an_explicit_subject_was_stored : Specification
{
    const string StoredSubject = "person-42";
    const string EventSource = "aggregate-1";

    EventEntry _entry;
    Subject _result;

    void Establish() => _entry = new EventEntry { EventSourceId = EventSource, Subject = StoredSubject };

    void Because() => _result = EventEntryConverter.GetSubject(_entry);

    [Fact] void should_return_the_stored_subject() => _result.Value.ShouldEqual(StoredSubject);
}
