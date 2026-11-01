// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_paged_event_sequence : an_event_sequence
{
    protected PageRequest _request;
    protected CallContext _callContext;
    protected EventSequencePageResponse _response = new();
    protected EventSequencePage _page;

    void Establish()
    {
        _sequences.Page(Arg.Any<PageRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<PageRequest>();
            _callContext = call.Arg<CallContext>();
            return QueryResult<EventSequencePageResponse>.Success(Guid.NewGuid(), _response);
        });
    }
}
