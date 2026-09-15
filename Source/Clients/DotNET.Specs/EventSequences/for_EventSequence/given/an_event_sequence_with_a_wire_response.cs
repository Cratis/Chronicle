// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_wire_response : an_event_sequence
{
    protected QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>> _wireResponse;

    protected void RespondWith(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>> response)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, response);
        stream.Position = 0;
        _wireResponse = Serializer.Deserialize<QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>>(stream);

        _sequences.FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), CallContext.Default).Returns(_wireResponse);
        _sequences.ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default).Returns(_wireResponse);
    }
}
