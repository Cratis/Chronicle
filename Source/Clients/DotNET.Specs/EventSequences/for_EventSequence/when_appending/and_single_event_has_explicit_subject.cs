// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_single_event_has_explicit_subject : given.an_event_sequence_with_metadata
{
    async Task Because() => await _eventSequence.Append(_source, "event", subject: (Subject)"person-1");

    [Fact] void should_notify_with_the_explicit_subject() => _notifications.Single().Event.Context.Subject.Value.ShouldEqual("person-1");
    [Fact] void should_not_report_the_subject_as_the_event_source_id() => _notifications.Single().Event.Context.SubjectIsEventSourceId.ShouldBeFalse();
}
