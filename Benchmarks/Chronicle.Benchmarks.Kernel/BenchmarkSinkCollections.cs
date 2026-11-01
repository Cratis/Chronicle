// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// An <see cref="ISinkCollections"/> that hands out a single collection.
/// </summary>
/// <param name="collection">The collection to hand out.</param>
public sealed class BenchmarkSinkCollections(IMongoCollection<BsonDocument> collection) : ISinkCollections
{
    /// <inheritdoc/>
    public string PromotingCollectionName => "benchmark-promoting";

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection() => collection;

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection(string collectionName) => collection;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public void AbandonReplay()
    {
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName collectionName) => Task.CompletedTask;
}
