// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SinkCollections"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the context is for.</param>
/// <param name="database">The <see cref="IMongoDatabase"/> to use.</param>
public class SinkCollections(
    Model model,
    IMongoDatabase database) : ISinkCollections
{
    bool _isReplaying;
    DateTimeOffset _replayStart;

    string ReplayCollectionName => $"replay-{model.Name}";

    /// <inheritdoc/>
    public async Task BeginReplay()
    {
        _isReplaying = true;
        _replayStart = DateTimeOffset.UtcNow;
        await PrepareInitialRun();
    }

    /// <inheritdoc/>
    public async Task EndReplay()
    {
        var rewindName = ReplayCollectionName;
        var rewoundCollectionsPrefix = $"{model.Name}-";
        var oldCollectionName = $"{rewoundCollectionsPrefix}{_replayStart:yyyyMMddHHmmss}";

        var collectionNames = (await database.ListCollectionNamesAsync()).ToList();
        if (collectionNames.Contains(model.Name))
        {
            await database.RenameCollectionAsync(model.Name, oldCollectionName);
        }

        if (collectionNames.Contains(rewindName))
        {
            await database.RenameCollectionAsync(rewindName, model.Name);
        }

        _isReplaying = false;
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun()
    {
        var collection = GetCollection();
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection() => _isReplaying ? database.GetCollection<BsonDocument>(ReplayCollectionName) : database.GetCollection<BsonDocument>(model.Name);
}
