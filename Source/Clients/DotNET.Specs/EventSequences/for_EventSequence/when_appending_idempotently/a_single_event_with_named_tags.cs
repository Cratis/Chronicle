// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_single_event_with_named_tags : given.an_idempotent_append
{
    async Task Because() => await _eventSequence.AppendOnceWithNamedTags("source", "event", _key, [new("category", "receipt")]);

    [Fact] void should_use_the_named_tag_transport() => _singleTaggedRequest.NamedTags.Single().Value.ShouldEqual("receipt");
    [Fact] void should_send_the_key() => _singleTaggedRequest.IdempotencyKey.ShouldEqual("order-1");
    [Fact] void should_not_use_the_untagged_transport() => _singleRequest.ShouldBeNull();
}
