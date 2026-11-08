// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries;

/// <summary>
/// Represents a <see cref="ISequenceQueryStorage"/> for saved event sequence queries in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class SequenceQueryStorage(IEventStoreDatabase eventStoreDatabase) : ISequenceQueryStorage
{
    IMongoCollection<SequenceQueryDefinition> Collection =>
        eventStoreDatabase.GetCollection<SequenceQueryDefinition>(WellKnownCollectionNames.SequenceQueries);

    IMongoCollection<SequenceQueryFolder> FolderCollection =>
        eventStoreDatabase.GetCollection<SequenceQueryFolder>(WellKnownCollectionNames.SequenceQueryFolders);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> GetAllFor(SequenceQueryOwner owner)
    {
        var ownerValue = owner.Value;
        using var result = await Collection.FindAsync(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue);
        var definitions = await result.ToListAsync();

        return definitions.Select(_ => _.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner)
    {
        var ownerValue = owner.Value;

        return new TransformingSubject<IEnumerable<SequenceQueryDefinition>, IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>>(
            Collection.Observe(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue),
            definitions => definitions.Select(_ => _.ToKernel()));
    }

    /// <inheritdoc/>
    public async Task<Concepts.SequenceQueries.SequenceQueryDefinition?> Get(SequenceQueryId id)
    {
        var value = id.Value;
        using var result = await Collection.FindAsync(_ => _.Id == value);
        var definition = await result.FirstOrDefaultAsync();

        return definition?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryFolderDefinition?> GetFolder(SequenceQueryFolderId id)
    {
        var value = id.Value;
        using var result = await FolderCollection.FindAsync(_ => _.Id == value);
        var folder = await result.FirstOrDefaultAsync();

        return folder?.ToKernel();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The driver's result is deliberately awaited and dropped, and only the outcome is returned.
    /// A caller that reflects over what it actually got - Arc does, when turning a command
    /// handler's return into a response - would otherwise surface a MongoDB driver type to the client.
    /// </remarks>
    public async Task<SequenceQueryWriteOutcome> Insert(Concepts.SequenceQueries.SequenceQueryDefinition definition) =>
        await InsertOnce(Collection, definition.ToMongoDB());

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> Update(Concepts.SequenceQueries.SequenceQueryDefinition definition, SequenceQueryOwner expectedOwner)
    {
        var id = definition.Id.Value;
        var owner = expectedOwner.Value;

        var result = await Collection.ReplaceOneAsync(
            filter: _ => _.Id == id && _.Owner == owner,
            replacement: (definition with { Owner = expectedOwner }).ToMongoDB());

        return result.MatchedCount == 0 ? SequenceQueryWriteOutcome.Conflict : SequenceQueryWriteOutcome.Applied;
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> Delete(SequenceQueryId id, SequenceQueryOwner expectedOwner)
    {
        var value = id.Value;
        var owner = expectedOwner.Value;

        var result = await Collection.DeleteOneAsync(_ => _.Id == value && _.Owner == owner);
        if (result.DeletedCount > 0) return SequenceQueryWriteOutcome.Applied;

        return await Get(id) is null ? SequenceQueryWriteOutcome.Applied : SequenceQueryWriteOutcome.Conflict;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SequenceQueryFolderDefinition>> GetAllFoldersFor(SequenceQueryOwner owner)
    {
        var ownerValue = owner.Value;
        using var result = await FolderCollection.FindAsync(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue);
        var folders = await result.ToListAsync();

        return folders.Select(_ => _.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> InsertFolder(SequenceQueryFolderDefinition definition) =>
        await InsertOnce(FolderCollection, definition.ToMongoDB());

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> UpdateFolder(SequenceQueryFolderDefinition definition, SequenceQueryOwner expectedOwner)
    {
        var id = definition.Id.Value;
        var owner = expectedOwner.Value;

        var result = await FolderCollection.ReplaceOneAsync(
            filter: _ => _.Id == id && _.Owner == owner,
            replacement: (definition with { Owner = expectedOwner }).ToMongoDB());

        return result.MatchedCount == 0 ? SequenceQueryWriteOutcome.Conflict : SequenceQueryWriteOutcome.Applied;
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> DeleteFolder(SequenceQueryFolderId id, SequenceQueryOwner expectedOwner)
    {
        var value = id.Value;
        var owner = expectedOwner.Value;

        var result = await FolderCollection.DeleteOneAsync(_ => _.Id == value && _.Owner == owner);
        if (result.DeletedCount > 0) return SequenceQueryWriteOutcome.Applied;

        return await GetFolder(id) is null ? SequenceQueryWriteOutcome.Applied : SequenceQueryWriteOutcome.Conflict;
    }

    static async Task<SequenceQueryWriteOutcome> InsertOnce<TDocument>(IMongoCollection<TDocument> collection, TDocument document)
    {
        try
        {
            await collection.InsertOneAsync(document);
            return SequenceQueryWriteOutcome.Applied;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return SequenceQueryWriteOutcome.Conflict;
        }
    }
}
