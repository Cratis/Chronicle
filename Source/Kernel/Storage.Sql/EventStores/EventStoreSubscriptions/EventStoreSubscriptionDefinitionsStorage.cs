// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions;

/// <summary>
/// Represents a <see cref="IEventStoreSubscriptionDefinitionsStorage"/> for event store subscription definitions in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class EventStoreSubscriptionDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IEventStoreSubscriptionDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var definitions = await scope.DbContext.EventStoreSubscriptions.ToListAsync();

        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(EventStoreSubscriptionId id)
    {
        await using var scope = await database.EventStore(eventStore);

        return await scope.DbContext.EventStoreSubscriptions.AnyAsync(s => s.Id == id.Value);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition?> Get(EventStoreSubscriptionId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.EventStoreSubscriptions.SingleOrDefaultAsync(s => s.Id == id.Value);

        return definition?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Delete(EventStoreSubscriptionId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.EventStoreSubscriptions.SingleOrDefaultAsync(s => s.Id == id.Value);

        if (definition is not null)
        {
            scope.DbContext.EventStoreSubscriptions.Remove(definition);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.EventStoreSubscriptions.Upsert(definition.ToSql());
        await scope.DbContext.SaveChangesAsync();
    }
}
