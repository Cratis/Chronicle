// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class with_a_failed_wire_response : given.an_asynchronous_event_sequence
{
    Exception _error;
    QueryResult<Contracts.Sequences.EventSequenceTailResponse> _response;

    void Establish()
    {
        _response = new() { ExceptionMessages = ["Synthetic tail failure"], ExceptionStackTrace = "Synthetic tail stack" };
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>()).Returns(_response);
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetTailSequenceNumberWithCancellation(_cancellation.Token).WaitAsync(TimeSpan.FromSeconds(10)));

    [Fact] void should_preserve_the_query_failure() => _error.ShouldBeOfExactType<QueryFailed>();
    [Fact] void should_preserve_the_exception_messages() => ((QueryFailed)_error).ExceptionMessages.ShouldEqual(_response.ExceptionMessages);
    [Fact] void should_preserve_the_originating_stack() => ((QueryFailed)_error).ExceptionStackTrace.ShouldEqual(_response.ExceptionStackTrace);
}
