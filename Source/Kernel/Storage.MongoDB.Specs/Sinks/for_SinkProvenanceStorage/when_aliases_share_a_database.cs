// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkProvenanceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_aliases_share_a_database(ReplicaSetMongoDBFixture fixture) : given.a_provenance_database(fixture)
{
    bool _initial;
    bool _alias;
    bool _recreated;
    bool _independent;
    string[] _collections;
    long _records;

    async Task Because()
    {
        _initial = await _storage.IsQuarantined();
        var alias = new SinkProvenanceStorage(_client.GetDatabase(_databaseName, new MongoDatabaseSettings { ReadPreference = ReadPreference.SecondaryPreferred }));
        await Task.WhenAll(Enumerable.Range(0, 8).Select(index => (index % 2 == 0 ? alias : _storage).MarkQuarantined()));
        _alias = await alias.IsQuarantined();
        _recreated = await new SinkProvenanceStorage(_client.GetDatabase(_databaseName)).IsQuarantined();
        _independent = await new SinkProvenanceStorage(_client.GetDatabase($"{_databaseName}_independent")).IsQuarantined();
        using var cursor = await _database.ListCollectionNamesAsync();
        _collections = (await cursor.ToListAsync()).ToArray();
        _records = await _metadata.CountDocumentsAsync(FilterDefinition<global::MongoDB.Bson.BsonDocument>.Empty);
    }

    [Fact] void should_allow_an_unmarked_ordinary_database() => _initial.ShouldBeFalse();
    [Fact] void should_share_the_mark_across_aliases_even_with_a_secondary_preference() => _alias.ShouldBeTrue();
    [Fact] void should_retain_the_mark_after_storage_recreation() => _recreated.ShouldBeTrue();
    [Fact] void should_not_affect_an_independent_database() => _independent.ShouldBeFalse();
    [Fact] void should_keep_one_mark_and_one_database_identity_after_concurrent_marks() => _records.ShouldEqual(2);
    [Fact] void should_not_create_an_ordinary_sink_collection() => _collections.ShouldEqual(new[] { SinkProvenanceNames.Metadata });
}
