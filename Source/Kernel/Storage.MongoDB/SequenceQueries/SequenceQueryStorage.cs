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
    /// <remarks>
    /// The driver's result is deliberately awaited and dropped rather than returned as the task's
    /// value. The interface promises a plain <see cref="Task"/>, and a caller that reflects over
    /// what it actually got - Arc does, when turning a command handler's return into a response -
    /// would otherwise surface a MongoDB driver type to the client.
    /// </remarks>
    public async Task Save(Concepts.SequenceQueries.SequenceQueryDefinition definition)
    {
        var id = definition.Id.Value;

        await Collection.ReplaceOneAsync(
            filter: _ => _.Id == id,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Awaited rather than returned for the same reason as <see cref="Save"/>.
    /// </remarks>
    public async Task Delete(SequenceQueryId id)
    {
        var value = id.Value;

        await Collection.DeleteOneAsync(_ => _.Id == value);
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
    /// <remarks>
    /// Awaited rather than returned for the same reason as <see cref="Save"/>.
    /// </remarks>
    public async Task SaveFolder(SequenceQueryFolderDefinition definition)
    {
        var id = definition.Id.Value;

        await FolderCollection.ReplaceOneAsync(
            filter: _ => _.Id == id,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Awaited rather than returned for the same reason as <see cref="Save"/>.
    /// </remarks>
    public async Task DeleteFolder(SequenceQueryFolderId id)
    {
        var value = id.Value;

        await FolderCollection.DeleteOneAsync(_ => _.Id == value);
    }
}
