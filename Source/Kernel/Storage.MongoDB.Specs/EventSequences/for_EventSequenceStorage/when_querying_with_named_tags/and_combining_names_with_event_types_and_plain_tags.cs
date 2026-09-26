// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_combining_names_with_event_types_and_plain_tags(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_storage_with_named_tags(fixture)
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCountMatching(new(EventTypes: [_eventType], Tags: [new Tag("important")])
    {
        NamedTags = [new(new TagName("account"), ["one"]), new(new TagName("region"), ["south"])]
    });

    [Fact] void should_match_any_named_criterion_and_every_other_dimension() => _count.Value.ShouldEqual(1UL);
}
