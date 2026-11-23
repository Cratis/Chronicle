// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkProvenanceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_mark_is_reloaded(ReplicaSetMongoDBFixture fixture) : given.a_provenance_database(fixture)
{
    readonly List<Exception?> _errors = [];
    Exception? _writeError;
    bool _reloaded;

    async Task Because()
    {
        await _storage.MarkQuarantined();
        _reloaded = await new SinkProvenanceStorage(_client.GetDatabase(_databaseName)).IsQuarantined();
        await Corrupt(new BsonDocument { { "_id", "protected" }, { "version", 99 }, { "restricted", true } });
        await Corrupt(new BsonDocument { { "_id", "protected" }, { "version", "1" }, { "restricted", true } });
        await Corrupt(new BsonDocument { { "_id", "protected" }, { "version", 1 }, { "restricted", false } });
        await Corrupt(new BsonDocument { { "_id", "unexpected" }, { "version", 1 }, { "restricted", true } });
        await _database.DropCollectionAsync(SinkProvenanceNames.Metadata);
        await _database.CreateViewAsync<BsonDocument, BsonDocument>(SinkProvenanceNames.Metadata, "source", Array.Empty<BsonDocument>());
        _writeError = await Catch.Exception(() => new SinkProvenanceStorage(_database).MarkQuarantined());
    }

    async Task Corrupt(BsonDocument record)
    {
        await _metadata.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await _metadata.InsertOneAsync(record);
        _errors.Add(await Catch.Exception(() => new SinkProvenanceStorage(_database).IsQuarantined()));
    }

    [Fact] void should_retain_a_valid_versioned_mark_after_recreation() => _reloaded.ShouldBeTrue();
    [Fact] void should_refuse_malformed_unsupported_or_unexpected_records() => _errors.TrueForAll(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_propagate_a_failed_durable_write() => _writeError.ShouldNotBeNull();
}
