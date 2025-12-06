// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Driver;
using ProjectionFutureMongo = Cratis.Chronicle.Storage.MongoDB.Projections.ProjectionFuture;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IProjectionFuturesStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
public class ProjectionFuturesStorage(IEventStoreNamespaceDatabase database, JsonSerializerOptions jsonSerializerOptions) : IProjectionFuturesStorage
{
    /// <inheritdoc/>
    public async Task Save(ProjectionId projectionId, Concepts.Projections.ProjectionFuture future)
    {
        var collection = database.GetCollection<ProjectionFutureMongo>(WellKnownCollectionNames.ProjectionFutures);
        var filter = Builders<ProjectionFutureMongo>.Filter.And(
            Builders<ProjectionFutureMongo>.Filter.Eq(f => f.ProjectionId, projectionId),
            Builders<ProjectionFutureMongo>.Filter.Eq(f => f.Id, future.Id));

        await collection.ReplaceOneAsync(
            filter,
            future.ToMongoDB(jsonSerializerOptions),
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Projections.ProjectionFuture>> GetForProjection(ProjectionId projectionId)
    {
        var collection = database.GetCollection<ProjectionFutureMongo>(WellKnownCollectionNames.ProjectionFutures);
        var filter = Builders<ProjectionFutureMongo>.Filter.Eq(f => f.ProjectionId, projectionId);

        var cursor = await collection.FindAsync(filter);
        var documents = await cursor.ToListAsync();

        return documents.Select(future => future.ToKernel(jsonSerializerOptions)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Remove(ProjectionId projectionId, ProjectionFutureId futureId)
    {
        var collection = database.GetCollection<ProjectionFutureMongo>(WellKnownCollectionNames.ProjectionFutures);
        var filter = Builders<ProjectionFutureMongo>.Filter.And(
            Builders<ProjectionFutureMongo>.Filter.Eq(f => f.ProjectionId, projectionId),
            Builders<ProjectionFutureMongo>.Filter.Eq(f => f.Id, futureId));

        await collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public async Task RemoveAllForProjection(ProjectionId projectionId)
    {
        var collection = database.GetCollection<ProjectionFutureMongo>(WellKnownCollectionNames.ProjectionFutures);
        var filter = Builders<ProjectionFutureMongo>.Filter.Eq(f => f.ProjectionId, projectionId);

        await collection.DeleteManyAsync(filter);
    }
}
