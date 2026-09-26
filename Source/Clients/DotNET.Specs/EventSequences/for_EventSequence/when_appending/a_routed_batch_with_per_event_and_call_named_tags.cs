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
            new EventForEventSourceId(_sourceId, "one") { NamedTags = [new("event", "one"), new("shared", "x")], Tags = ["first"], EventSourceType = "customer", EventStreamType = "orders", EventStreamId = "current", Subject = "invoice", Occurred = _occurred },
            new EventForEventSourceId(EventSourceId.New(), "two") { NamedTags = [new("event", "two")] }
        ],
        [new("shared", "x"), new("call", "z")]);

    [Fact] void should_use_tagged_routed_rpc() => _routedRequest.ShouldNotBeNull();
    [Fact] void should_merge_event_first_then_call_and_deduplicate_pairs() => _routedRequest.Events.First().NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("event", "one"), ("shared", "x"), ("call", "z")]);
    [Fact] void should_not_bleed_event_tags_to_other_events() => _routedRequest.Events.Last().NamedTags.Select(_ => _.Value).ShouldEqual(["two", "x", "z"]);
    [Fact] void should_keep_plain_tags_separate() => _routedRequest.Events.First().Tags.ShouldContain("first");
    [Fact] void should_send_the_first_events_route() => (_routedRequest.Events.First().EventSourceId, _routedRequest.Events.First().EventSourceType, _routedRequest.Events.First().EventStreamType, _routedRequest.Events.First().EventStreamId).ShouldEqual((_sourceId.Value, "customer", "orders", "current"));
    [Fact] void should_send_the_first_events_subject() => _routedRequest.Events.First().Subject.ShouldEqual("invoice");
    [Fact] void should_send_the_sequence_identifier() => _routedRequest.EventSequenceId.ShouldEqual(_eventSequence.Id.Value);
    [Fact] void should_send_the_serialized_content() => _routedRequest.Events.All(_ => _.Content == "{}").ShouldBeTrue();
    [Fact] void should_send_the_identity() => _routedRequest.CausedBy.ShouldNotBeNull();
    [Fact] void should_preserve_occurred_time() => _routedRequest.Events.First().Occurred.ToString().ShouldEqual(_occurred.ToString("O"));
    [Fact] void should_notify_with_each_events_own_named_tags() => _observedContexts.Select(_ => _.NamedTags.First().Value).ShouldEqual(["one", "two"]);
    [Fact] void should_not_call_legacy_rpc() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default);
}
