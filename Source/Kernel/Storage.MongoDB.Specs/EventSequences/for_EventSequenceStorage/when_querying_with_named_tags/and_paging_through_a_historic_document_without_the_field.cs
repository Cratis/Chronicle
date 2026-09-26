// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_paging_through_a_historic_document_without_the_field(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_storage_with_named_tags(fixture)
{
    AppendedEvent _historicEvent;

    async Task Because()
    {
        using var cursor = await _storage.GetPage(new EventSequenceQueryCriteria(EventSourceId: "historic-source"), 0, 10);
        await cursor.MoveNext();
        _historicEvent = cursor.Current.Single();
    }

    [Fact] void should_read_the_event_with_no_named_tags() => _historicEvent.Context.NamedTags.ShouldBeEmpty();
    [Fact] void should_have_unset_the_named_tags_field_in_storage() => _historicDocument.Contains("namedTags").ShouldBeFalse();
}
