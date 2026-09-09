// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_event_sequence_indexes;

[Collection(MongoDBCollection.Name)]
public class when_ensuring_indexes_on_a_fresh_collection(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    string _collectionName;
    IReadOnlyList<string> _indexes;

    async Task Because()
    {
        _collectionName = _database.GetEventSequenceCollectionFor(EventSequenceId.Log).CollectionNamespace.CollectionName;
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _indexes = await IndexNamesFor(_collectionName);
    }

    [Fact] void should_create_the_per_type_tail_index() => _indexes.ShouldContain("type_sequenceNumber");
    [Fact] void should_keep_the_event_source_and_type_compound() => _indexes.ShouldContain("eventSourceId_eventTypeId");
    [Fact] void should_keep_the_tags_index() => _indexes.ShouldContain("tags");
    [Fact] void should_not_create_the_single_field_event_type_index() => _indexes.ShouldNotContain("eventTypeId");
    [Fact] void should_not_create_the_single_field_event_stream_type_index() => _indexes.ShouldNotContain("eventStreamType");
    [Fact] void should_not_create_the_event_source_id_text_index() => _indexes.ShouldNotContain("eventSourceId");
    [Fact] void should_not_create_the_content_hashes_wildcard_index() => _indexes.ShouldNotContain("contentHashes");
}
