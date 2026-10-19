// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_batch_with_named_tags : given.an_idempotent_append
{
    async Task Because() => await _eventSequence.AppendManyOnceWithNamedTags("source", ["one", "two"], _key, [new("category", "receipt")]);

    [Fact] void should_use_the_named_tag_transport() => _batchTaggedRequest.Events.All(_ => _.Event.NamedTags.Single().Value == "receipt").ShouldBeTrue();
    [Fact] void should_key_each_event() => _batchTaggedRequest.Events.Select(_ => _.IdempotencyKey).ToArray().ShouldEqual([_key.ForIndex(0).Value, _key.ForIndex(1).Value]);
}
