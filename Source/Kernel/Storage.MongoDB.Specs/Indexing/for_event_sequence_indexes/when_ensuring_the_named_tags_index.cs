// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_event_sequence_indexes;

[Collection(MongoDBCollection.Name)]
public class when_ensuring_the_named_tags_index(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    string _collectionName;
    IReadOnlyList<string> _indexes;
    BsonDocument _keys;

    async Task Because()
    {
        _collectionName = _database.GetEventSequenceCollectionFor(EventSequenceId.Log).CollectionNamespace.CollectionName;
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _indexes = await IndexNamesFor(_collectionName);

        // The keys must match the stored element names; a key on a differently cased path would index nothing.
        _keys = _indexes.Contains("namedTags_name_value") ? (await IndexFor(_collectionName, "namedTags_name_value"))["key"].AsBsonDocument : [];
    }

    [Fact] void should_create_the_named_tags_index() => _indexes.ShouldContain("namedTags_name_value");

    [Fact] void should_index_the_stored_name_then_value_fields() => string.Join(',', _keys.Names).ShouldEqual("namedTags.name,namedTags.value");
    [Fact] void should_index_both_fields_ascending() => string.Join(',', _keys.Values.Select(_ => _.ToInt32())).ShouldEqual("1,1");
}
