// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_single_source_batch_with_named_tags : given.a_named_tag_append
{
    EventContext[] _observedContexts;

    void Establish() => _eventSequence.AppendOperations.Subscribe(events => _observedContexts = events.Select(_ => _.Event.Context).ToArray());

    async Task Because() => await _eventSequence.AppendMany(_sourceId, ["one", "two"], [new("name", "a"), new("name", "a"), new("name", "b")], tags: ["plain"], subject: "invoice");

    [Fact] void should_use_tagged_batch_rpc() => _batchRequest.ShouldNotBeNull();
    [Fact] void should_include_distinct_pairs_on_every_event() => _batchRequest.Events.All(_ => _.NamedTags.Select(tag => tag.Value).SequenceEqual(["a", "b"])).ShouldBeTrue();
    [Fact] void should_send_the_source_and_sequence_identifiers() => (_batchRequest.EventSourceId, _batchRequest.EventSequenceId).ShouldEqual((_sourceId.Value, _eventSequence.Id.Value));
    [Fact] void should_send_plain_tags_without_named_tags() => _batchRequest.Tags.ShouldContain("plain");
    [Fact] void should_send_the_subject_on_each_event() => _batchRequest.Events.All(_ => _.Subject == "invoice").ShouldBeTrue();
    [Fact] void should_send_the_content_on_each_event() => _batchRequest.Events.All(_ => _.Content == "{}").ShouldBeTrue();
    [Fact] void should_send_the_identity() => _batchRequest.CausedBy.ShouldNotBeNull();
    [Fact] void should_notify_each_event_with_its_named_tags() => _observedContexts.All(_ => _.NamedTags.Count() == 2).ShouldBeTrue();
    [Fact] void should_not_call_legacy_rpc() => _sequences.DidNotReceive().AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), CallContext.Default);
}
