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
    const int NamespaceNotFound = 26;
    const int NamespaceExists = 48;

    bool _isReplaying;

    /// <inheritdoc/>
    public string PromotingCollectionName => $"replay-{readModel.ContainerName}-promoting";

    string ReplayCollectionName => $"replay-{readModel.ContainerName}";

    /// <inheritdoc/>
    public async Task BeginReplay(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        _isReplaying = true;

        // A promotion that was cut short between claiming the replay collection and renaming it into
        // place leaves the claimed collection behind. It belongs to a replay that is being superseded,
        // and left there it would make this replay's own claim fail.
        await DropIfExists(PromotingCollectionName);
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
        // Ending a replay is sent to every silo, and every silo holds its own sink for the read model.
        // Whatever happens to the swap, this sink must stop writing to the replay collection: a sink left
        // in replay mode keeps sending every live change to a collection nothing reads, with no error to
        // show for it (#4296).
        try
        {
            await PromoteReplayCollection(context);
        }
        finally
        {
            _isReplaying = false;
        }
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

    static bool IsNamespaceConflict(MongoCommandException exception) =>
        exception.Code is NamespaceNotFound or NamespaceExists;

    async Task PromoteReplayCollection(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        // Only perform the rename swap when the replay collection contains documents. If the
        // replay produced zero writes (e.g. the job's PrepareSteps observed no keys yet because
        // the event index hadn't caught up), renaming main → revert without a populated
        // replacement would wipe the existing read model — turning a transient race into
        // permanent data loss. Leaving main untouched in that case correctly treats the empty
        // replay as a no-op.
        if (!await CollectionHasDocuments(ReplayCollectionName))
        {
            await DropIfExists(ReplayCollectionName);
            return;
        }

        if (!await TryClaimReplayCollection())
        {
            return;
        }

        if (await CollectionExists(readModel.ContainerName))
        {
            try
            {
                await database.RenameCollectionAsync(readModel.ContainerName, context.RevertContainerName);
            }
            catch
            {
                // Hand the replay back so that a later end of the replay can still promote it.
                await database.RenameCollectionAsync(PromotingCollectionName, ReplayCollectionName);
                throw;
            }
        }

        await database.RenameCollectionAsync(PromotingCollectionName, readModel.ContainerName);
    }

    /// <summary>
    /// Claims the replay collection for promotion by renaming it aside. A rename is atomic, so when
    /// several silos end the same replay at once exactly one of them wins the claim and performs the
    /// swap; the others find the replay collection gone and leave the read model to it. Without the
    /// claim, two silos could each move the main collection aside, and the second would move away the
    /// collection the first had just promoted.
    /// </summary>
    /// <returns>True when this call claimed the replay collection and must promote it.</returns>
    async Task<bool> TryClaimReplayCollection()
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await database.RenameCollectionAsync(ReplayCollectionName, PromotingCollectionName);
                return true;
            }
            catch (MongoCommandException exception) when (IsNamespaceConflict(exception))
            {
                if (!await CollectionExists(ReplayCollectionName))
                {
                    // Another silo claimed it first and is promoting it.
                    return false;
                }

                // The replay collection is still here, so what is in the way is a claimed collection a
                // cut-short promotion left behind. It is older than this replay - drop it and claim again.
                await DropIfExists(PromotingCollectionName);
            }
        }

        return false;
    }

    async Task<bool> CollectionExists(string name)
    {
        var filter = new BsonDocument("name", name);
        var names = await (await database.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filter })).ToListAsync();
        return names.Count > 0;
    }

    async Task<bool> CollectionHasDocuments(string name) =>
        await CollectionExists(name) &&
        await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(1).AnyAsync();

    async Task DropIfExists(string name)
    {
        if (await CollectionExists(name))
        {
            await database.DropCollectionAsync(name);
        }
    }
}
