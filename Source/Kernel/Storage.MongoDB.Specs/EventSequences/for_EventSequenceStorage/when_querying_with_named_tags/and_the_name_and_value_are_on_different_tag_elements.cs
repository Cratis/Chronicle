// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_name_and_value_are_on_different_tag_elements(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_storage_with_named_tags(fixture)
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCountMatching(new()
    {
        NamedTags = [new(new TagName("account"), ["north"])]
    });

    [Fact] void should_not_match_across_tag_elements() => _count.Value.ShouldEqual(0UL);
}
