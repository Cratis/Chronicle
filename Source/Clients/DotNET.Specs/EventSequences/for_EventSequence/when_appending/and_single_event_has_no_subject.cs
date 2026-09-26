// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_single_event_has_no_subject : given.an_event_sequence_with_metadata
{
    async Task Because() => await _eventSequence.Append(_source, "event");

    [Fact] void should_notify_with_the_event_source_id_as_subject() => _notifications.Single().Event.Context.Subject.Value.ShouldEqual(_source.Value);
    [Fact] void should_report_the_subject_as_the_event_source_id() => _notifications.Single().Event.Context.SubjectIsEventSourceId.ShouldBeTrue();
}
