// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_named_tag_append : an_event_sequence
{
    protected EventSourceId _sourceId;
    protected Contracts.Sequences.AppendWithNamedTagsRequest _singleRequest;
    protected Contracts.Sequences.AppendManyWithNamedTagsRequest _batchRequest;
    protected Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest _routedRequest;

    void Establish()
    {
        _sourceId = EventSourceId.New();
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType(Guid.NewGuid().ToString(), EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _sequences.When(_ => _.AppendWithNamedTags(Arg.Any<Contracts.Sequences.AppendWithNamedTagsRequest>(), CallContext.Default))
            .Do(call => _singleRequest = call.Arg<Contracts.Sequences.AppendWithNamedTagsRequest>());
        _sequences.When(_ => _.AppendManyWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyWithNamedTagsRequest>(), CallContext.Default))
            .Do(call => _batchRequest = call.Arg<Contracts.Sequences.AppendManyWithNamedTagsRequest>());
        _sequences.When(_ => _.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default))
            .Do(call => _routedRequest = call.Arg<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>());
        _sequences.AppendWithNamedTags(Arg.Any<Contracts.Sequences.AppendWithNamedTagsRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new Contracts.Sequences.AppendResponse
            {
                SequenceNumber = 42,
                ConstraintViolations = [],
                Errors = []
            }));
        _sequences.AppendManyWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyWithNamedTagsRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), BatchResponse()));
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), BatchResponse()));
    }

    static Contracts.Sequences.AppendManyResponse BatchResponse() => new()
    {
        SequenceNumbers = [42, 43],
        ConstraintViolations = [],
        ConcurrencyViolations = [],
        Errors = []
    };
}
