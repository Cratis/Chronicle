// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_single_event_has_resolved_subject : given.an_event_sequence_with_metadata
{
    void Establish()
    {
        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(new EventType("subject-event", EventTypeGeneration.First));
    }

    async Task Because() => await _eventSequence.Append(_source, new EventWithSubject("person-2"));

    [Fact] void should_notify_with_the_resolved_subject() => _notifications.Single().Event.Context.Subject.Value.ShouldEqual("person-2");
    [Fact] void should_not_report_the_subject_as_the_event_source_id() => _notifications.Single().Event.Context.SubjectIsEventSourceId.ShouldBeFalse();

    record EventWithSubject([Subject] string PersonId);
}
