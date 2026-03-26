// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReducerDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IReducerDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reducers.ReducerDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var reducers = await scope.DbContext.Reducers.ToListAsync();
        return reducers.Select(reducer => reducer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ReducerId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Reducers.AnyAsync(reducer => reducer.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Reducers.ReducerDefinition> Get(ReducerId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var reducer = await scope.DbContext.Reducers
            .Where(reducer => reducer.Id == id)
            .Select(reducer => reducer.ToKernel())
            .FirstOrDefaultAsync();

        return reducer!;
    }

    /// <inheritdoc/>
    public async Task Delete(ReducerId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var projection = await scope.DbContext.Reducers.FindAsync(id);
        if (projection != null)
        {
            scope.DbContext.Reducers.Remove(projection);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Reducers.ReducerDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = definition.ToSql();
        await scope.DbContext.Reducers.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
