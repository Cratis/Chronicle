// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Monads;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_kernel_reports_already_completed : given.an_event_sequence
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    void Establish() => _sequences.CompleteStreamScope(Arg.Any<Contracts.Sequences.CompleteStreamScopeRequest>(), CallContext.Default)
        .Returns(CommandResult<Contracts.Sequences.CompleteStreamResponse>.Success(Guid.NewGuid(), new()
        {
            IsSuccess = false,
            Error = Contracts.Sequences.CompleteStreamError.AlreadyCompleted
        }));

    async Task Because() => _result = await _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source"));

    [Fact] void should_report_already_completed() => _result.AsT1.ShouldEqual(CompleteStreamError.AlreadyCompleted);
}
