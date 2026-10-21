// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_named_tags_in_the_owner_commit : given.a_unit_with_native_sequence
{
    Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest _taggedRequest;

    async Task Because()
    {
        using var frame = Refuse();
        await _sequence.Transactional.AppendWithNamedTags("source", "changed", [new("key", "value")]);
        ClearCalls();
        await frame.Commit();
        _taggedRequest = (Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest)_sequences.ReceivedCalls().Single().GetArguments()[0];
    }

    [Fact] async Task should_commit_the_tagged_batch_exactly_once() => await _sequences.Received(1).AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), Arg.Any<CallContext>());
    [Fact] void should_preserve_named_tags() => _taggedRequest.Events.Single().NamedTags.Single().Value.ShouldEqual("value");
    [Fact] void should_complete_successfully() => _unit.IsSuccess.ShouldBeTrue();
}
