// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_kernel_reports_an_unknown_error : given.an_event_sequence
{
    Exception _error;

    void Establish() => _sequences.CompleteStreamScope(Arg.Any<Contracts.Sequences.CompleteStreamScopeRequest>(), CallContext.Default)
        .Returns(CommandResult<Contracts.Sequences.CompleteStreamResponse>.Success(Guid.NewGuid(), new()
        {
            IsSuccess = false,
            Error = (Contracts.Sequences.CompleteStreamError)99
        }));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source")));

    [Fact] void should_throw_a_named_error() => _error.ShouldBeOfExactType<UnknownCompleteStreamError>();
}
