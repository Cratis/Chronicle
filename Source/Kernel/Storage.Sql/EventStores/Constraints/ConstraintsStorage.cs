// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage.</param>
public class ConstraintsStorage(EventStoreName eventStore, IDatabase database) : IConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<IConstraintDefinition>> GetDefinitions()
    {
        await using var scope = await database.EventStore(eventStore);

        // Pull rows then group/order client-side: ulong is not translatable in ORDER BY on the
        // SQLite provider, and the version history per name is small enough that materializing
        // the full table is fine. Translating via cast would diverge between providers and
        // narrowing Version to a signed integer would silently shift behavior in the kernel.
        var constraints = await scope.DbContext.Constraints.ToListAsync();
        return constraints
            .GroupBy(_ => _.Name)
            .Select(_ => _.OrderByDescending(c => c.Version).First())
            .Select(c => c.ToKernel())
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task SaveDefinition(IConstraintDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);

        // Materialize before sorting — SQLite EF cannot translate OrderByDescending on a ulong.
        var existingByName = await scope.DbContext.Constraints
            .Where(_ => _.Name == definition.Name.Value)
            .ToListAsync();
        var existing = existingByName.OrderByDescending(_ => _.Version).FirstOrDefault();

        if (existing?.ToKernel().Equals(definition) == true)
        {
            return;
        }

        var nextVersion = existing is null ? 1uL : existing.Version + 1;
        var entity = definition.ToSql(nextVersion);
        scope.DbContext.Constraints.Add(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
