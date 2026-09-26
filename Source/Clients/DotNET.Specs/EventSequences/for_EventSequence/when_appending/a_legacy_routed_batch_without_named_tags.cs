// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_legacy_routed_batch_without_named_tags : given.a_named_tag_append
{
    Contracts.Sequences.AppendManyForEventSourcesRequest _request;

    void Establish()
    {
        _sequences.When(_ => _.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default))
            .Do(call => _request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>());
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new()
            {
                SequenceNumbers = [42], ConstraintViolations = [], ConcurrencyViolations = [], Errors = []
            }));
    }

    async Task Because() => await _eventSequence.AppendMany([new EventForEventSourceId(_sourceId, "one")], tags: ["plain"]);

    [Fact] void should_send_the_same_legacy_request_metadata() => _request.EventSequenceId.ShouldEqual(_eventSequence.Id.Value);
    [Fact] void should_send_the_same_legacy_event_fields() => (_request.Events.Single().EventSourceId, _request.Events.Single().Content, _request.Events.Single().Tags.Single()).ShouldEqual((_sourceId.Value, "{}", "plain"));
    [Fact] void should_not_use_tagged_rpc() => _sequences.DidNotReceive().AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default);
}
