// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_non_append_operations : given.a_unit_with_native_sequence
{
    Dictionary<string, Exception> _errors;

    async Task Because()
    {
        _sequences.Revise(Arg.Any<Contracts.Sequences.ReviseRequest>(), Arg.Any<CallContext>()).Returns(CommandResult.Success(Guid.NewGuid()));
        _sequences.Redact(Arg.Any<Contracts.Sequences.RedactRequest>(), Arg.Any<CallContext>()).Returns(CommandResult.Success(Guid.NewGuid()));
        _sequences.RedactForEventSource(Arg.Any<Contracts.Sequences.RedactForEventSourceRequest>(), Arg.Any<CallContext>()).Returns(CommandResult.Success(Guid.NewGuid()));
        var completion = CommandResult<Contracts.Sequences.CompleteStreamResponse>.Success(Guid.NewGuid(), new() { IsSuccess = true, SequenceNumber = 102 });
        _sequences.CompleteStream(Arg.Any<Contracts.Sequences.CompleteStreamRequest>(), Arg.Any<CallContext>()).Returns(completion);
        _sequences.CompleteStreamScope(Arg.Any<Contracts.Sequences.CompleteStreamScopeRequest>(), Arg.Any<CallContext>()).Returns(completion);
        using var owner = Refuse();
        ClearCalls();
        _errors = new()
        {
            ["revision"] = await Catch.Exception(() => _sequence.Revise(EventSequenceNumber.First, "changed")),
            ["redact position"] = await Catch.Exception(() => _sequence.Redact(EventSequenceNumber.First, "reason")),
            ["redact source"] = await Catch.Exception(() => _sequence.Redact((EventSourceId)"source", "reason", typeof(string))),
            ["complete stream"] = await Catch.Exception(() => _sequence.CompleteStream("stream", "id")),
            ["complete scope"] = await Catch.Exception(() => _sequence.CompleteStream(ClosedStreamScope.ForEventSource("source")))
        };
    }

    [Fact] void should_not_change_revision_redaction_or_completion_admission() => _errors.Values.ShouldEachConformTo(error => error is null);
    [Fact] void should_cover_each_unmodified_policy() => _errors.Count.ShouldEqual(5);
    [Fact] void should_send_each_non_append_operation() => _sequences.ReceivedCalls().Count().ShouldEqual(5);
}
