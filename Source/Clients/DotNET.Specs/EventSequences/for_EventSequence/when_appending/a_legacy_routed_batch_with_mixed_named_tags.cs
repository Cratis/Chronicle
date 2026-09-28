// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_legacy_routed_batch_with_mixed_named_tags : given.a_named_tag_append
{
    async Task Because() => await _eventSequence.AppendMany(
        [new EventForEventSourceId(_sourceId, "one") { NamedTags = [new("name", "value")] }, new EventForEventSourceId(EventSourceId.New(), "two")]);

    [Fact] void should_use_tagged_rpc() => _routedRequest.ShouldNotBeNull();
    [Fact] void should_preserve_the_first_events_named_tags() => _routedRequest.Events.First().NamedTags.Single().Value.ShouldEqual("value");
    [Fact] void should_send_empty_named_tags_for_the_second_event() => _routedRequest.Events.Last().NamedTags.ShouldBeEmpty();
    [Fact] void should_not_use_legacy_rpc() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default);
}
