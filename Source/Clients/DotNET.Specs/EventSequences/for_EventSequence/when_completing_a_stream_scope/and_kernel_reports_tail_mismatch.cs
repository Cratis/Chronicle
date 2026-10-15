// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Monads;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_kernel_reports_tail_mismatch : given.an_event_sequence
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    void Establish() => _sequences.CompleteStreamScope(Arg.Any<Contracts.Sequences.CompleteStreamScopeRequest>(), CallContext.Default)
        .Returns(CommandResult<Contracts.Sequences.CompleteStreamResponse>.Success(Guid.NewGuid(), new()
        {
            IsSuccess = false,
            Error = Contracts.Sequences.CompleteStreamError.ExpectedTailMismatch
        }));

    async Task Because() => _result = await _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source"), new EventSequenceNumber(42));

    [Fact] void should_report_tail_mismatch() => _result.AsT1.ShouldEqual(CompleteStreamError.ExpectedTailMismatch);
    [Fact] async Task should_send_scope_and_expected_tail() => await _sequences.Received(1).CompleteStreamScope(Arg.Is<Contracts.Sequences.CompleteStreamScopeRequest>(request => request.EventSourceId == "source" && request.EventStreamId == null && request.ExpectedTailSequenceNumber == 42), CallContext.Default);
}
