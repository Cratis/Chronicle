// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections;

/// <summary>
/// Represents a SQL implementation of <see cref="IProjectionFuturesStorage"/>.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
public class ProjectionFuturesStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database, JsonSerializerOptions jsonSerializerOptions) : IProjectionFuturesStorage
{
    /// <inheritdoc/>
    public async Task Save(ProjectionId projectionId, ProjectionFuture future)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entity = await scope.DbContext.ProjectionFutures
            .FirstOrDefaultAsync(f => f.ProjectionId == projectionId.Value && f.Id == future.Id.Value.ToString());

        if (entity is null)
        {
            entity = ProjectionFuturesConverters.ToEntity(projectionId, future, jsonSerializerOptions);
            scope.DbContext.ProjectionFutures.Add(entity);
        }
        else
        {
            var updated = ProjectionFuturesConverters.ToEntity(projectionId, future, jsonSerializerOptions);
            entity.EventSequenceNumber = updated.EventSequenceNumber;
            entity.EventTypeId = updated.EventTypeId;
            entity.EventTypeGeneration = updated.EventTypeGeneration;
            entity.EventSourceId = updated.EventSourceId;
            entity.EventContentJson = updated.EventContentJson;
            entity.ParentPath = updated.ParentPath;
            entity.ChildPath = updated.ChildPath;
            entity.IdentifiedByProperty = updated.IdentifiedByProperty;
            entity.ParentIdentifiedByProperty = updated.ParentIdentifiedByProperty;
            entity.ParentKeyJson = updated.ParentKeyJson;
            entity.Created = updated.Created;
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionFuture>> GetForProjection(ProjectionId projectionId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entities = await scope.DbContext.ProjectionFutures
            .Where(f => f.ProjectionId == projectionId.Value)
            .ToListAsync();

        return entities.Select(entity => ProjectionFuturesConverters.ToProjectionFuture(entity, jsonSerializerOptions)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Remove(ProjectionId projectionId, ProjectionFutureId futureId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entity = await scope.DbContext.ProjectionFutures
            .FirstOrDefaultAsync(f => f.ProjectionId == projectionId.Value && f.Id == futureId.Value.ToString());

        if (entity is not null)
        {
            scope.DbContext.ProjectionFutures.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAllForProjection(ProjectionId projectionId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entities = await scope.DbContext.ProjectionFutures
            .Where(f => f.ProjectionId == projectionId.Value)
            .ToListAsync();

        if (entities.Count > 0)
        {
            scope.DbContext.ProjectionFutures.RemoveRange(entities);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
