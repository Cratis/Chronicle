// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_legacy_routed_batch_with_named_tags : given.a_named_tag_append
{
    async Task Because() => await _eventSequence.AppendMany(
        [new EventForEventSourceId(_sourceId, "one") { NamedTags = [new("name", "value"), new("name", "value")] }],
        tags: ["plain"]);

    [Fact] void should_use_tagged_rpc() => _routedRequest.ShouldNotBeNull();
    [Fact] void should_preserve_and_deduplicate_named_tags() => _routedRequest.Events.Single().NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("name", "value")]);
    [Fact] void should_keep_plain_tags() => _routedRequest.Events.Single().Tags.ShouldContain("plain");
    [Fact] void should_not_use_legacy_rpc() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default);
}
