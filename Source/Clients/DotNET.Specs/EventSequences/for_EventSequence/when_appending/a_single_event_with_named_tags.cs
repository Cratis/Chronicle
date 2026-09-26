// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_single_event_with_named_tags : given.a_named_tag_append
{
    EventContext _observedContext;

    void Establish() => _eventSequence.AppendOperations.Subscribe(events => _observedContext = events.Single().Event.Context);

    async Task Because() => await _eventSequence.Append(_sourceId, "one", [new("Case", ""), new("Case", ""), new("case", "value")], eventStreamType: "orders", eventStreamId: "current", eventSourceType: "customer", tags: ["plain"], subject: "invoice");

    [Fact] void should_use_the_tagged_rpc() => _singleRequest.ShouldNotBeNull();
    [Fact] void should_deduplicate_exact_pairs_without_folding_case() => _singleRequest.NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("Case", ""), ("case", "value")]);
    [Fact] void should_preserve_plain_tags() => _singleRequest.Tags.ShouldContain("plain");
    [Fact] void should_send_the_event_source_and_route() => (_singleRequest.EventSourceId, _singleRequest.EventSourceType, _singleRequest.EventStreamType, _singleRequest.EventStreamId).ShouldEqual((_sourceId.Value, "customer", "orders", "current"));
    [Fact] void should_send_the_subject() => _singleRequest.Subject.ShouldEqual("invoice");
    [Fact] void should_send_the_sequence_identifier() => _singleRequest.EventSequenceId.ShouldEqual(_eventSequence.Id.Value);
    [Fact] void should_send_the_serialized_content() => _singleRequest.Content.ShouldEqual("{}");
    [Fact] void should_send_the_event_type() => _singleRequest.EventType.Id.ShouldNotBeEmpty();
    [Fact] void should_send_the_identity() => _singleRequest.CausedBy.ShouldNotBeNull();
    [Fact] void should_retain_named_tags_in_local_notifications() => _observedContext.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("Case", ""), ("case", "value")]);
    [Fact] void should_not_call_legacy_rpc() => _sequences.DidNotReceive().Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default);
}
