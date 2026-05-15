// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReactorDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IReactorDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reactors.ReactorDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var reactors = await scope.DbContext.Reactors.ToListAsync();
        return reactors.Select(reactor => reactor.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ReactorId id)
    {
        var idValue = id.Value;
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Reactors.AnyAsync(reactor => reactor.Id == idValue);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Reactors.ReactorDefinition> Get(ReactorId id)
    {
        var idValue = id.Value;
        await using var scope = await database.EventStore(eventStore);
        var entity = await scope.DbContext.Reactors
            .Where(r => r.Id == idValue)
            .FirstOrDefaultAsync();
        return entity?.ToKernel()!;
    }

    /// <inheritdoc/>
    public async Task Delete(ReactorId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var projection = await scope.DbContext.Reactors.FindAsync(id);
        if (projection != null)
        {
            scope.DbContext.Reactors.Remove(projection);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Reactors.ReactorDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = definition.ToSql();
        await scope.DbContext.Reactors.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Rename(ReactorId currentId, ReactorId newId)
    {
        await using var scope = await database.EventStore(eventStore);
        var existing = await scope.DbContext.Reactors.FindAsync(currentId);
        if (existing is null)
        {
            return;
        }

        var renamed = new ReactorDefinition
        {
            Id = newId,
            Owner = existing.Owner,
            EventSequenceId = existing.EventSequenceId,
            EventTypes = existing.EventTypes,
            IsReplayable = existing.IsReplayable
        };

        scope.DbContext.Reactors.Remove(existing);
        await scope.DbContext.Reactors.AddAsync(renamed);
        await scope.DbContext.SaveChangesAsync();
    }
}
