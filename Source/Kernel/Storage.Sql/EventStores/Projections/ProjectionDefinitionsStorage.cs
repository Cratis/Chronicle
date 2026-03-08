// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage.</param>
public class ProjectionDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IProjectionDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var projections = await scope.DbContext.Projections.ToListAsync();
        return projections.Select(projection => projection.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ProjectionId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Projections.AnyAsync(projection => projection.Id == id);
    }

    /// <inheritdoc/>
    public async Task<ProjectionDefinition> Get(ProjectionId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var projection = await scope.DbContext.Projections
            .Where(projection => projection.Id == id)
            .Select(projection => projection.ToKernel())
            .FirstOrDefaultAsync();

        return projection!;
    }

    /// <inheritdoc/>
    public async Task Delete(ProjectionId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var projection = await scope.DbContext.Projections.FindAsync(id);
        if (projection != null)
        {
            scope.DbContext.Projections.Remove(projection);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = definition.ToSql();
        await scope.DbContext.Projections.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
