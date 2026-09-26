// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_single_event_with_named_tags : given.a_named_tag_append
{
    EventContext _observedContext;

    void Establish() => _eventSequence.AppendOperations.Subscribe(events => _observedContext = events.Single().Event.Context);

    async Task Because() => await _eventSequence.Append(_sourceId, "one", [new("Case", ""), new("Case", ""), new("case", "value")], tags: ["plain"]);

    [Fact] void should_use_the_tagged_rpc() => _singleRequest.ShouldNotBeNull();
    [Fact] void should_deduplicate_exact_pairs_without_folding_case() => _singleRequest.NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("Case", ""), ("case", "value")]);
    [Fact] void should_preserve_plain_tags() => _singleRequest.Tags.ShouldContain("plain");
    [Fact] void should_retain_named_tags_in_local_notifications() => _observedContext.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("Case", ""), ("case", "value")]);
    [Fact] void should_not_call_legacy_rpc() => _sequences.DidNotReceive().Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default);
}
