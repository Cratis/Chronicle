// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_counting_with_multiple_names(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_storage_with_named_tags(fixture)
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCountMatching(new()
    {
        NamedTags = [new(new TagName("account"), ["one"]), new(new TagName("region"), ["south"])]
    });

    [Fact] void should_match_any_criterion() => _count.Value.ShouldEqual(3UL);
}
