// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_routed_batch_with_per_event_and_call_named_tags : given.a_named_tag_append
{
    EventContext[] _observedContexts;
    readonly DateTimeOffset _occurred = DateTimeOffset.UtcNow;

    void Establish() => _eventSequence.AppendOperations.Subscribe(events => _observedContexts = events.Select(_ => _.Event.Context).ToArray());

    async Task Because() => await _eventSequence.AppendMany(
        [
            new EventForEventSourceId(_sourceId, "one") { NamedTags = [new("event", "one"), new("shared", "x")], Tags = ["first"], Occurred = _occurred },
            new EventForEventSourceId(EventSourceId.New(), "two") { NamedTags = [new("event", "two")] }
        ],
        [new("shared", "x"), new("call", "z")]);

    [Fact] void should_use_tagged_routed_rpc() => _routedRequest.ShouldNotBeNull();
    [Fact] void should_merge_event_first_then_call_and_deduplicate_pairs() => _routedRequest.Events.First().NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("event", "one"), ("shared", "x"), ("call", "z")]);
    [Fact] void should_not_bleed_event_tags_to_other_events() => _routedRequest.Events.Last().NamedTags.Select(_ => _.Value).ShouldEqual(["two", "x", "z"]);
    [Fact] void should_keep_plain_tags_separate() => _routedRequest.Events.First().Tags.ShouldContain("first");
    [Fact] void should_preserve_occurred_time() => _routedRequest.Events.First().Occurred.ToString().ShouldEqual(_occurred.ToString("O"));
    [Fact] void should_notify_with_each_events_own_named_tags() => _observedContexts.Select(_ => _.NamedTags.First().Value).ShouldEqual(["one", "two"]);
    [Fact] void should_not_call_legacy_rpc() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default);
}
