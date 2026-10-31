// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the accepted, non-throwing path of the fenced MongoDB sink's retry-safety check. No server is contacted.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class FencedSinkRetrySafetyBenchmark
{
    const int MaxBatchSize = 1000;

    MongoClient _client = null!;
    IMongoCollection<BsonDocument> _collection = null!;
    UpdateDefinition<BsonDocument> _update = null!;
    List<WriteModel<BsonDocument>> _requests = null!;

    /// <summary>
    /// Creates the collection and the accepted update and bulk requests.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _client = new MongoClient("mongodb://localhost:27017");
        _collection = _client.GetDatabase("benchmark").GetCollection<BsonDocument>("benchmark");
        _update = Builders<BsonDocument>.Update.Set("name", "value").Unset("obsolete");
        _requests = [];
        for (var i = 0; i < 10; i++)
        {
            _requests.Add(new UpdateOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", i),
                Builders<BsonDocument>.Update.Set("name", $"value{i}")));
        }
    }

    /// <summary>
    /// Releases the client.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    /// <summary>
    /// Checks a single idempotent update.
    /// </summary>
    [Benchmark]
    public void SingleUpdate() => SinkBatchDocuments.RequireRetrySafe(_collection, _update, MaxBatchSize);

    /// <summary>
    /// Checks a bulk of ten keyed updates.
    /// </summary>
    [Benchmark]
    public void BulkUpdates() => SinkBatchDocuments.RequireRetrySafe(_collection, _requests, MaxBatchSize);
}
