// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>
/// Initializes catalog collections before transactions.
/// </summary>
internal static class MaterializationBindingCollections
{
    /// <summary>
    /// The authoritative binding collection.
    /// </summary>
    internal const string Bindings = "materialization-bindings";

    /// <summary>
    /// The durable publication receipt collection.
    /// </summary>
    internal const string Receipts = "materialization-publication-receipts";

    /// <summary>The immutable execution collection.</summary>
    internal const string Executions = "materialization-executions";

    /// <summary>The explicit observer claim collection.</summary>
    internal const string Observers = "materialization-observer-bindings";

    /// <summary>The durable reservation operation collection.</summary>
    internal const string Operations = "materialization-build-operations";

    /// <summary>The physical destination claim collection.</summary>
    internal const string Destinations = "materialization-destination-claims";

    static readonly ConcurrentDictionary<(IMongoClient Client, string Database), Lazy<Task>> _ensured = new();
    static readonly ConcurrentDictionary<(IMongoClient Client, string Database), Lazy<Task>> _buildsEnsured = new();

    /// <summary>Initializes companion collections only when the build API is used.</summary>
    /// <param name="database">The database.</param>
    /// <returns>The initialization task.</returns>
    internal static async Task EnsureBuilds(IMongoDatabase database)
    {
        var key = (database.Client, database.DatabaseNamespace.DatabaseName);
        try
        {
            await Ensure(database);
            await _buildsEnsured.GetOrAdd(key, static (_, db) => new(() => CreateBuilds(db)), database).Value;
        }
        catch
        {
            _buildsEnsured.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <returns>The collection initialization task.</returns>
    internal static async Task Ensure(IMongoDatabase database)
    {
        var key = (database.Client, database.DatabaseNamespace.DatabaseName);
        try
        {
            await _ensured.GetOrAdd(key, static (_, db) => new(() => Create(db)), database).Value;
        }
        catch
        {
            _ensured.TryRemove(key, out _);
            throw;
        }
    }

    static async Task CreateBuilds(IMongoDatabase database)
    {
        foreach (var collection in new[] { Executions, Observers, Operations, Destinations })
        {
            try
            {
                await database.CreateCollectionAsync(collection);
            }
            catch (MongoCommandException error) when (error.Code == 48)
            {
            }
        }
    }

    static async Task Create(IMongoDatabase database)
    {
        foreach (var collection in new[] { Bindings, Receipts, GenerationGateCollections.Gates })
        {
            try
            {
                await database.CreateCollectionAsync(collection);
            }
            catch (MongoCommandException error) when (error.Code == 48)
            {
                // Another initializer already created the collection.
            }
        }
        await database.GetCollection<BsonDocument>(Bindings).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("generations.destination")));
    }
}
