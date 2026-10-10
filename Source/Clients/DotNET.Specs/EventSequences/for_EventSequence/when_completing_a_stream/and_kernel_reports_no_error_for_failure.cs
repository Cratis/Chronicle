// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Monads;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream;

public class and_kernel_reports_no_error_for_failure : given.an_event_sequence
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    void Establish() => _sequences.CompleteStream(Arg.Any<Contracts.Sequences.CompleteStreamRequest>(), CallContext.Default)
        .Returns(CommandResult<Contracts.Sequences.CompleteStreamResponse>.Success(Guid.NewGuid(), new()
        {
            IsSuccess = false,
            Error = Contracts.Sequences.CompleteStreamError.None
        }));

    async Task Because() => _result = await _eventSequence.CompleteStream("transactions", "month");

    [Fact] void should_preserve_the_legacy_already_completed_mapping() => _result.AsT1.ShouldEqual(CompleteStreamError.AlreadyCompleted);
}
