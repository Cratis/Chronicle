// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage.</param>
public class ReadModelDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IReadModelDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.ReadModels.ReadModelDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var readModels = await scope.DbContext.ReadModels.ToListAsync();
        return readModels.Select(rm => rm.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ReadModelIdentifier identifier)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.ReadModels.AnyAsync(rm => rm.Id == identifier);
    }

    /// <inheritdoc/>
    public async Task<Concepts.ReadModels.ReadModelDefinition> Get(ReadModelIdentifier identifier)
    {
        await using var scope = await database.EventStore(eventStore);
        var readModel = await scope.DbContext.ReadModels
            .Where(rm => rm.Id == identifier)
            .Select(rm => rm.ToKernel())
            .FirstOrDefaultAsync();

        return readModel!;
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.ReadModels.ReadModelDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = definition.ToSql();
        await scope.DbContext.ReadModels.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(ReadModelIdentifier identifier)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = await scope.DbContext.ReadModels.FindAsync(identifier);
        if (entity != null)
        {
            scope.DbContext.ReadModels.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
