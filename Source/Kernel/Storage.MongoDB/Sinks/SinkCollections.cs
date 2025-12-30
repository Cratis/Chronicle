// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
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
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the context is for.</param>
/// <param name="database">The <see cref="IMongoDatabase"/> to use.</param>
public class SinkCollections(
    ReadModelDefinition readModel,
    IMongoDatabase database) : ISinkCollections
{
    bool _isReplaying;
    string ReplayCollectionName => $"replay-{readModel.Name}";

    /// <inheritdoc/>
    public async Task BeginReplay(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        _isReplaying = true;
        await PrepareInitialRun();
    }

    /// <inheritdoc/>
    public Task ResumeReplay(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        _isReplaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task EndReplay(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        var rewindName = ReplayCollectionName;

        var collectionNames = (await database.ListCollectionNamesAsync()).ToList();
        if (collectionNames.Contains(readModel.Name))
        {
            await database.RenameCollectionAsync(readModel.Name, context.RevertReadModel);
        }

        if (collectionNames.Contains(rewindName))
        {
            await database.RenameCollectionAsync(rewindName, readModel.Name);
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
    public IMongoCollection<BsonDocument> GetCollection() => _isReplaying ? database.GetCollection<BsonDocument>(ReplayCollectionName) : database.GetCollection<BsonDocument>(readModel.Name);

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection(string collectionName) => database.GetCollection<BsonDocument>(collectionName);
}
