// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class with_a_transport_failure : given.an_event_sequence
{
    RpcException _transportError;
    Exception _error;

    void Establish()
    {
        _transportError = new(new Status(StatusCode.Unavailable, "Synthetic transport failure"));
        _sequences.FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), CallContext.Default)
            .Returns(Task.FromException<QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>>(_transportError));
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First));

    [Fact] void should_preserve_the_transport_failure() => _error.ShouldEqual(_transportError);
}
