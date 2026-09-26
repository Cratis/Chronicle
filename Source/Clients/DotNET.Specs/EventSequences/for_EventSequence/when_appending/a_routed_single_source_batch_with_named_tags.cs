// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_routed_single_source_batch_with_named_tags : given.a_named_tag_append
{
    async Task Because() => await _eventSequence.AppendMany(_sourceId, ["one", "two"], [new("key", "value")], eventStreamType: "other", concurrencyScope: ConcurrencyScope.None);

    [Fact] void should_route_through_tagged_multi_source_rpc() => _routedRequest.ShouldNotBeNull();
    [Fact] void should_carry_named_tags_for_every_event() => _routedRequest.Events.All(_ => _.NamedTags.Single().Value == "value").ShouldBeTrue();
    [Fact] void should_preserve_routing() => _routedRequest.Events.All(_ => _.EventStreamType == "other").ShouldBeTrue();
}
