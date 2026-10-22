// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Coordinates tracked upserts with saving, so a concurrent insert becomes an update of the same primary key.
/// </summary>
internal static class SqlUpserts
{
    static readonly ConditionalWeakTable<DbContext, HashSet<object>> _inserts = new();
    static readonly ConditionalWeakTable<object, Action<object>> _collisionMerges = new();

    /// <summary>
    /// Stage an upsert, remembering which inserts may converge with a concurrent writer.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="set">The entity set.</param>
    /// <param name="entity">The desired values.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Upsert<TEntity>(this DbSet<TEntity> set, TEntity entity)
        where TEntity : class
    {
        await Microsoft.EntityFrameworkCore.DbSetExtensions.Upsert(set, entity);
        var entry = set.Entry(entity);
        if (entry.State == EntityState.Added)
        {
            _inserts.GetValue(entry.Context, _ => new(ReferenceEqualityComparer.Instance)).Add(entity);
        }
    }

    /// <summary>
    /// Stage a known-missing row, merging persisted values if another writer inserts it first.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="set">The entity set.</param>
    /// <param name="entity">The desired values.</param>
    /// <param name="mergeCollision">The merge to apply when an insert loses a race.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Insert<TEntity>(this DbSet<TEntity> set, TEntity entity, Action<TEntity, TEntity> mergeCollision)
        where TEntity : class
    {
        await set.AddAsync(entity);
        _inserts.GetValue(set.Entry(entity).Context, _ => new(ReferenceEqualityComparer.Instance)).Add(entity);
        _collisionMerges.Add(entity, existing => mergeCollision(entity, (TEntity)existing));
    }

    /// <summary>
    /// Save staged changes, recovering only unique collisions on inserts explicitly requested as upserts.
    /// </summary>
    /// <param name="context">The context saving the changes.</param>
    /// <param name="save">The context's normal save operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of saved entries.</returns>
    internal static async Task<int> SaveChanges(DbContext context, Func<Task<int>> save, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                return await save();
            }
            catch (DbUpdateException error) when (StorageWriteConflicts.IsUnique(error))
            {
                if (!_inserts.TryGetValue(context, out var inserts) || error.Entries.Count == 0 ||
                    error.Entries.Any(entry => entry.State != EntityState.Added || !inserts.Contains(entry.Entity)))
                {
                    throw;
                }

                var recovered = false;
                foreach (var entry in error.Entries)
                {
                    var existing = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (existing is null)
                    {
                        continue;
                    }

                    if (_collisionMerges.TryGetValue(entry.Entity, out var merge))
                    {
                        merge(existing.ToObject());
                    }

                    entry.OriginalValues.SetValues(existing);
                    entry.State = EntityState.Modified;
                    inserts.Remove(entry.Entity);
                    recovered = true;
                }

                // A secondary unique key is not proof that this primary key exists. Do not hide that conflict.
                // Each retry removes at least one Added entry, bounding recovery by the staged insert count.
                if (!recovered)
                {
                    throw;
                }
            }
        }
    }
}
