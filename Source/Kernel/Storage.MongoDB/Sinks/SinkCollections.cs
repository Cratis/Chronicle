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
    string ReplayCollectionName => $"replay-{readModel.ContainerName}";

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

        var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();

        // Only perform the rename swap when the replay collection contains documents. If the
        // replay produced zero writes (e.g. the job's PrepareSteps observed no keys yet because
        // the event index hadn't caught up), renaming main → revert without a populated
        // replacement would wipe the existing read model — turning a transient race into
        // permanent data loss. Leaving main untouched in that case correctly treats the empty
        // replay as a no-op.
        var replayCollectionExists = collectionNames.Contains(rewindName);
        var replayHasDocuments = replayCollectionExists
            && await database.GetCollection<BsonDocument>(rewindName)
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Limit(1)
                .AnyAsync();
        if (!replayHasDocuments)
        {
            if (replayCollectionExists)
            {
                await database.DropCollectionAsync(rewindName);
            }
            _isReplaying = false;
            return;
        }

        if (collectionNames.Contains(readModel.ContainerName))
        {
            await database.RenameCollectionAsync(readModel.ContainerName, context.RevertContainerName);
        }

        await database.RenameCollectionAsync(rewindName, readModel.ContainerName);

        _isReplaying = false;
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun()
    {
        var collection = GetCollection();
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public async Task Remove(ReadModelContainerName collectionName)
    {
        var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();
        if (collectionNames.Contains(collectionName.Value))
        {
            await database.DropCollectionAsync(collectionName);
        }
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection() => _isReplaying ? database.GetCollection<BsonDocument>(ReplayCollectionName) : database.GetCollection<BsonDocument>(readModel.ContainerName);

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection(string collectionName) => database.GetCollection<BsonDocument>(collectionName);
}
