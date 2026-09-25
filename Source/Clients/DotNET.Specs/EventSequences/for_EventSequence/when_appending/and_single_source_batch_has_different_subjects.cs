// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_single_source_batch_has_different_subjects : given.an_event_sequence_with_metadata
{
    Contracts.Sequences.AppendManyRequest _command;

    void Establish()
    {
        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(new EventType("subject-event", EventTypeGeneration.First));
        _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _command = call.Arg<Contracts.Sequences.AppendManyRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, new() { CorrelationId = _correlationId, SequenceNumbers = [42, 43], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
        });
    }

    async Task Because() => await _eventSequence.AppendMany(_source, [new EventWithSubject("person-1"), "without subject"]);

    [Fact] void should_send_the_resolved_subject() => _command.Events.First().Subject.ShouldEqual("person-1");
    [Fact] void should_notify_with_the_resolved_subject() => _notifications[0].Event.Context.Subject.Value.ShouldEqual("person-1");
    [Fact] void should_send_no_subject_for_the_other_event() => _command.Events.Last().Subject.ShouldBeNull();
    [Fact] void should_notify_with_the_event_source_id_for_the_other_event() => _notifications[1].Event.Context.Subject.Value.ShouldEqual(_source.Value);
    [Fact] void should_report_only_the_other_subject_as_the_event_source_id() => _notifications.Select(_ => _.Event.Context.SubjectIsEventSourceId).ToArray().ShouldEqual([false, true]);

    record EventWithSubject([Subject] string PersonId);
}
