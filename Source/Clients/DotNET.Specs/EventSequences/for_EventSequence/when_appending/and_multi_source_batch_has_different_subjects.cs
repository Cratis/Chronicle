// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_multi_source_batch_has_different_subjects : given.an_event_sequence_with_metadata
{
    EventSourceId _otherSource;
    EventSourceId _thirdSource;

    void Establish()
    {
        _otherSource = "other-source";
        _thirdSource = "third-source";
        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(new EventType("subject-event", EventTypeGeneration.First));
    }

    async Task Because() => await _eventSequence.AppendMany(
    [
        new EventForEventSourceId(_source, new EventWithSubject("person-1")),
        new EventForEventSourceId(_otherSource, "explicit") { Subject = "person-2" },
        new EventForEventSourceId(_thirdSource, "without subject")
    ]);

    [Fact] void should_send_the_resolved_subject() => _batchRequest.Events.ElementAt(0).Subject.ShouldEqual("person-1");
    [Fact] void should_send_the_explicit_subject() => _batchRequest.Events.ElementAt(1).Subject.ShouldEqual("person-2");
    [Fact] void should_send_no_subject_for_the_last_event() => _batchRequest.Events.ElementAt(2).Subject.ShouldBeNull();
    [Fact] void should_notify_each_event_with_its_own_subject() => _notifications.Select(_ => _.Event.Context.Subject.Value).ToArray().ShouldEqual(["person-1", "person-2", _thirdSource.Value]);
    [Fact] void should_report_only_the_last_subject_as_the_event_source_id() => _notifications.Select(_ => _.Event.Context.SubjectIsEventSourceId).ToArray().ShouldEqual([false, false, true]);

    record EventWithSubject([Subject] string PersonId);
}
