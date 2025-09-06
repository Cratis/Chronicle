// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ReactorDefinitionsStorage(ReactorDefinitionsDbContext dbContext) : IReactorDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reactors.ReactorDefinition>> GetAll()
    {
        var reactors = await dbContext.Reactors.ToListAsync();
        return reactors.Select(reactor => reactor.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReactorId id) =>
        dbContext.Reactors.AnyAsync(reactor => reactor.Id == id);

    /// <inheritdoc/>
    public Task<Concepts.Observation.Reactors.ReactorDefinition> Get(ReactorId id) =>
        dbContext.Reactors
            .Where(reactor => reactor.Id == id)
            .Select(reactor => reactor.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Delete(ReactorId id)
    {
        var projection = await dbContext.Reactors.FindAsync(id);
        if (projection != null)
        {
            dbContext.Reactors.Remove(projection);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Reactors.ReactorDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.Reactors.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}
