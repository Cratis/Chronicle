// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ProjectionDefinitionsStorage(ProjectionsDbContext dbContext) : IProjectionDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var projections = await dbContext.Projections.ToListAsync();
        return projections.Select(projection => projection.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) =>
        dbContext.Projections.AnyAsync(projection => projection.Id == id);

    /// <inheritdoc/>
    public Task<ProjectionDefinition> Get(ProjectionId id) =>
        dbContext.Projections
            .Where(projection => projection.Id == id)
            .Select(projection => projection.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Delete(ProjectionId id)
    {
        var projection = await dbContext.Projections.FindAsync(id);
        if (projection != null)
        {
            dbContext.Projections.Remove(projection);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.Projections.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}
