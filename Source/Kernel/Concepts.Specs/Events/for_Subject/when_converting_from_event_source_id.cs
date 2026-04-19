// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_Subject;

public class when_converting_from_event_source_id : Specification
{
    EventSourceId _eventSourceId;
    Subject _subject;

    void Establish() => _eventSourceId = "user-42";

    void Because() => _subject = _eventSourceId;

    [Fact] void should_carry_event_source_id_value() => _subject.Value.ShouldEqual("user-42");
    [Fact] void should_be_considered_set() => _subject.IsSet.ShouldBeTrue();
}
