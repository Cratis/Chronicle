// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ReducerDefinitionsStorage(EventStoreDbContext dbContext) : IReducerDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reducers.ReducerDefinition>> GetAll()
    {
        var reducers = await dbContext.Reducers.ToListAsync();
        return reducers.Select(reducer => reducer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReducerId id) =>
        dbContext.Reactors.AnyAsync(reactor => reactor.Id == id);

    /// <inheritdoc/>
    public Task<Concepts.Observation.Reducers.ReducerDefinition> Get(ReducerId id) =>
        dbContext.Reducers
            .Where(reducer => reducer.Id == id)
            .Select(reducer => reducer.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Delete(ReducerId id)
    {
        var projection = await dbContext.Reducers.FindAsync(id);
        if (projection != null)
        {
            dbContext.Reducers.Remove(projection);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Reducers.ReducerDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.Reducers.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}
