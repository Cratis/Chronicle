// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_counting_by_name_without_a_value(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_storage_with_named_tags(fixture)
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCountMatching(new()
    {
        NamedTags = [new(new TagName("account"))]
    });

    [Fact] void should_match_every_value_of_the_name_but_not_the_historic_event() => _count.Value.ShouldEqual(4UL);
    [Fact] void should_have_unset_the_historic_named_tags_field() => _historicDocument.Contains("namedTags").ShouldBeFalse();
}
