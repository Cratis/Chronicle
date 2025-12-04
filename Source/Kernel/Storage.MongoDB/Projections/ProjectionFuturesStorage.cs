// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IProjectionFuturesStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
public class ProjectionFuturesStorage(IEventStoreNamespaceDatabase database) : IProjectionFuturesStorage
{
    const string CollectionName = "projection-futures";

    /// <inheritdoc/>
    public async Task Save(ProjectionId projectionId, Key key, ProjectionFuture future)
    {
        var collection = database.GetCollection<ProjectionFuture>(CollectionName);
        var filter = Builders<ProjectionFuture>.Filter.And(
            Builders<ProjectionFuture>.Filter.Eq(f => f.ProjectionId, projectionId),
            Builders<ProjectionFuture>.Filter.Eq(f => f.Id, future.Id));

        await collection.ReplaceOneAsync(
            filter,
            future,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionFuture>> GetForProjectionAndKey(ProjectionId projectionId, Key key)
    {
        var collection = database.GetCollection<ProjectionFuture>(CollectionName);
        var filter = Builders<ProjectionFuture>.Filter.Eq(f => f.ProjectionId, projectionId);

        var cursor = await collection.FindAsync(filter);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task Remove(ProjectionId projectionId, Key key, ProjectionFutureId futureId)
    {
        var collection = database.GetCollection<ProjectionFuture>(CollectionName);
        var filter = Builders<ProjectionFuture>.Filter.And(
            Builders<ProjectionFuture>.Filter.Eq(f => f.ProjectionId, projectionId),
            Builders<ProjectionFuture>.Filter.Eq(f => f.Id, futureId));

        await collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public async Task RemoveAllForProjectionAndKey(ProjectionId projectionId, Key key)
    {
        var collection = database.GetCollection<ProjectionFuture>(CollectionName);
        var filter = Builders<ProjectionFuture>.Filter.Eq(f => f.ProjectionId, projectionId);

        await collection.DeleteManyAsync(filter);
    }
}
