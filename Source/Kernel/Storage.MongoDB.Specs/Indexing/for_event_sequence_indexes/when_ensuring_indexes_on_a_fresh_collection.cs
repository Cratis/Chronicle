// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_event_sequence_indexes;

[Collection(MongoDBCollection.Name)]
public class when_ensuring_indexes_on_a_fresh_collection(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    string _collectionName;
    BsonDocument _eventMutationIndex;
    BsonDocument _historyMutationIndex;
    IReadOnlyList<string> _historyIndexes;
    IReadOnlyList<string> _indexes;

    async Task Because()
    {
        _collectionName = _database.GetEventSequenceCollectionFor(EventSequenceId.Log).CollectionNamespace.CollectionName;
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _indexes = await IndexNamesFor(_collectionName);
        _eventMutationIndex = await IndexFor(_collectionName, "lastMutationOrdinal_sequenceNumber");
        _historyIndexes = await IndexNamesFor(WellKnownCollectionNames.EventSequenceMutationHistory);
        _historyMutationIndex = await IndexFor(WellKnownCollectionNames.EventSequenceMutationHistory, "eventSequenceId_ordinal");
    }

    [Fact] void should_create_the_per_type_tail_index() => _indexes.ShouldContain("type_sequenceNumber");
    [Fact] void should_create_the_event_mutation_index() => _indexes.ShouldContain("lastMutationOrdinal_sequenceNumber");
    [Fact] void should_name_the_event_mutation_index() => _eventMutationIndex["name"].AsString.ShouldEqual("lastMutationOrdinal_sequenceNumber");
    [Fact] void should_use_the_exact_event_mutation_index_keys() => _eventMutationIndex["key"].AsBsonDocument.ShouldEqual(new BsonDocument { ["lastMutationOrdinal"] = 1, ["_id"] = 1 });
    [Fact] void should_make_the_event_mutation_index_non_sparse() => _eventMutationIndex.GetValue("sparse", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_create_the_history_mutation_index() => _historyIndexes.ShouldContain("eventSequenceId_ordinal");
    [Fact] void should_not_create_a_redundant_history_mutation_id_index() => _historyIndexes.ShouldContainOnly(["_id_", "eventSequenceId_ordinal"]);
    [Fact] void should_name_the_history_mutation_index() => _historyMutationIndex["name"].AsString.ShouldEqual("eventSequenceId_ordinal");
    [Fact] void should_use_the_exact_history_mutation_index_keys() => _historyMutationIndex["key"].AsBsonDocument.ShouldEqual(new BsonDocument { ["eventSequenceId"] = 1, ["ordinal"] = 1 });
    [Fact] void should_make_the_history_mutation_index_unique() => _historyMutationIndex.GetValue("unique", false).AsBoolean.ShouldBeTrue();
    [Fact] void should_make_the_history_mutation_index_non_sparse() => _historyMutationIndex.GetValue("sparse", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_keep_the_event_source_and_type_compound() => _indexes.ShouldContain("eventSourceId_eventTypeId");
    [Fact] void should_keep_the_tags_index() => _indexes.ShouldContain("tags");
    [Fact] void should_not_create_the_single_field_event_type_index() => _indexes.ShouldNotContain("eventTypeId");
    [Fact] void should_not_create_the_single_field_event_stream_type_index() => _indexes.ShouldNotContain("eventStreamType");
    [Fact] void should_not_create_the_event_source_id_text_index() => _indexes.ShouldNotContain("eventSourceId");
    [Fact] void should_not_create_the_content_hashes_wildcard_index() => _indexes.ShouldNotContain("contentHashes");
}
