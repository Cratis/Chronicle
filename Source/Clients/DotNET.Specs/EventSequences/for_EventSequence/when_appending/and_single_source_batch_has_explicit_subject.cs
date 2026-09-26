// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_single_source_batch_has_explicit_subject : given.an_event_sequence_with_metadata
{
    void Establish() => _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>())
        .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, new() { CorrelationId = _correlationId, SequenceNumbers = [42, 43], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] }));

    async Task Because() => await _eventSequence.AppendMany(_source, ["first", "second"], subject: (Subject)"explicit-person");

    [Fact] void should_notify_each_event_with_the_explicit_subject() => _notifications.Select(_ => _.Event.Context.Subject.Value).ToArray().ShouldEqual(["explicit-person", "explicit-person"]);
    [Fact] void should_not_report_either_subject_as_the_event_source_id() => _notifications.All(_ => !_.Event.Context.SubjectIsEventSourceId).ShouldBeTrue();
}
