// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
/// <param name="logger">Optional <see cref="ILogger{T}"/> for logging refused destructive operations.</param>
/// <param name="generationClaims">Optional generation ownership lookup.</param>
/// <remarks>
/// Every destructive operation - clearing, dropping and renaming - is guarded by <see cref="SinkCollectionGuards"/>, so it only
/// ever touches the collection this sink owns in its current lifecycle phase, and fails loudly otherwise.
/// </remarks>
public class SinkCollections(
    ReadModelDefinition readModel,
    IMongoDatabase database,
    ILogger<SinkCollections>? logger = null,
    IGenerationContainerClaims? generationClaims = null) : ISinkCollections
{
    const int NamespaceNotFound = 26;
    const int NamespaceExists = 48;

    readonly SinkCollectionNames _names = SinkCollectionNames.For(readModel.ContainerName);
    readonly ILogger<SinkCollections> _logger = logger ?? NullLogger<SinkCollections>.Instance;
    bool _isReplaying;

    /// <inheritdoc/>
    public string PromotingCollectionName => _names.Promoting;

    string ReplayCollectionName => _names.Replay;

    /// <inheritdoc/>
    public async Task BeginReplay(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        _isReplaying = true;

        // A promotion that was cut short between claiming the replay collection and renaming it into
        // place leaves the claimed collection behind. It belongs to a replay that is being superseded,
        // and left there it would make this replay's own claim fail.
        await DropIfExists(PromotingCollectionName);

        // Clear what this replay owns: the replay shadow. Should the mode have been changed underneath this call, the
        // collection resolved would be the primary, and the guard refuses rather than wiping the live read model.
        await Clear(GetCollection(), isReplaying: true);
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
    public void AbandonReplay() => _isReplaying = false;

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Clear(GetCollection(), _isReplaying);

    /// <inheritdoc/>
    public async Task Remove(ReadModelContainerName collectionName)
    {
        Guard(SinkCollectionOperation.Drop, collectionName.Value, () => SinkCollectionGuards.ThrowIfCannotRemove(_names, collectionName.Value));
        await GuardGenerations(SinkCollectionOperation.Drop, collectionName.Value);
        var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();
        if (collectionNames.Contains(collectionName.Value))
        {
            await database.DropCollectionAsync(collectionName);
        }
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection() => database.GetCollection<BsonDocument>(_isReplaying ? ReplayCollectionName : _names.Primary);

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

        Guard(SinkCollectionOperation.Rename, _names.Primary, () => SinkCollectionGuards.ThrowIfCannotMovePrimaryAside(_names, context.RevertContainerName));

        if (!await TryClaimReplayCollection())
        {
            return;
        }

        var claimed = await CollectionUuid(PromotingCollectionName);
        if (await CollectionExists(_names.Primary))
        {
            try
            {
                await Rename(_names.Primary, context.RevertContainerName);
            }
            catch
            {
                // Hand the replay back so that a later end of the replay can still promote it - but only when the
                // promoting collection is still the one this promotion claimed, never another replay's. A refused
                // hand-back is logged by the guard; the failure that got us here is the one rethrown.
                try
                {
                    await ThrowIfPromotingCollectionChanged(claimed);
                    await Rename(PromotingCollectionName, ReplayCollectionName);
                }
                catch (SinkCollectionOperationRefused)
                {
                }

                throw;
            }
        }

        await ThrowIfPromotingCollectionChanged(claimed);
        await Rename(PromotingCollectionName, _names.Primary);
    }

    async Task ThrowIfPromotingCollectionChanged(BsonValue? claimed)
    {
        var current = await CollectionUuid(PromotingCollectionName);
        Guard(SinkCollectionOperation.Rename, PromotingCollectionName, () => SinkCollectionGuards.ThrowIfPromotingCollectionChanged(_names, claimed, current));
    }

    async Task Clear(IMongoCollection<BsonDocument> collection, bool isReplaying)
    {
        var name = collection.CollectionNamespace.CollectionName;
        Guard(SinkCollectionOperation.Clear, name, () => SinkCollectionGuards.ThrowIfCannotClear(_names, isReplaying, name));
        await GuardGenerations(SinkCollectionOperation.Clear, name);
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    async Task Rename(string source, string target)
    {
        await GuardGenerations(SinkCollectionOperation.Rename, source);
        await GuardGenerations(SinkCollectionOperation.Rename, target);
        await database.RenameCollectionAsync(source, target);
    }

    async Task GuardGenerations(SinkCollectionOperation operation, string collection)
    {
        if (generationClaims is not null)
        {
            var claims = await generationClaims.For(collection);
            Guard(operation, collection, () => SinkCollectionGuards.ThrowIfGenerationProtected(operation, collection, _names.Primary, claims));
        }
    }

    void Guard(SinkCollectionOperation operation, string collection, Action guard)
    {
        try
        {
            guard();
        }
        catch (SinkCollectionOperationRefused exception)
        {
            _logger.SinkCollectionOperationRefused(operation, collection, readModel.Identifier, exception);
            throw;
        }
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
                await Rename(ReplayCollectionName, PromotingCollectionName);
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

    async Task<BsonValue?> CollectionUuid(string name)
    {
        var options = new ListCollectionsOptions { Filter = new BsonDocument("name", name) };
        var collections = await (await database.ListCollectionsAsync(options)).ToListAsync();
        return collections.Count > 0 && collections[0].TryGetValue("info", out var info) && info.AsBsonDocument.TryGetValue("uuid", out var uuid)
            ? uuid
            : null;
    }

    async Task<bool> CollectionHasDocuments(string name) =>
        await CollectionExists(name) &&
        await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(1).AnyAsync();

    async Task DropIfExists(string name)
    {
        Guard(SinkCollectionOperation.Drop, name, () => SinkCollectionGuards.ThrowIfCannotDropDuringReplay(_names, name));
        await GuardGenerations(SinkCollectionOperation.Drop, name);
        if (await CollectionExists(name))
        {
            await database.DropCollectionAsync(name);
        }
    }
}
