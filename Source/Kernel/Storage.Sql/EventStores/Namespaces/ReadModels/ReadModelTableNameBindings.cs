// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Persists reservations in the resolved read-model placement. It does not cache decisions.
/// </summary>
/// <param name="context">The dedicated metadata context, owned by the caller.</param>
/// <param name="schema">The resolved schema for exact catalog probes.</param>
/// <param name="migrator">The existing migration executor.</param>
internal sealed class ReadModelTableNameBindings(
    ReadModelTableBindingsDbContext context,
    string schema,
    ITableMigrator<ReadModelTableBindingsDbContext> migrator) : IReadModelTableNameBindings
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public async Task<ReadModelTableNameBinding> UnderLock(Func<Task<ReadModelTableNameBinding>> action)
    {
        var semaphore = _locks.GetOrAdd($"{context.Database.GetConnectionString()}:{schema}", _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            ReadModelTableNameBinding? result = null;
            await DatabaseSchemaLock.Run(context, async () =>
            {
                await new ReadModelTableBindingsMigration(migrator).Ensure(context, schema);
                result = await action();
            });
            return result!;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public Task<ReadModelTableNameBinding?> Find(string id) => context.Bindings.AsNoTracking().SingleOrDefaultAsync(binding => binding.Id == id);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReadModelTableNameBinding>> Read() => await context.Bindings.AsNoTracking().ToListAsync();

    /// <inheritdoc/>
    public async Task<bool> TryInsert(ReadModelTableNameBinding binding)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A table-name reservation must commit before provisioning, outside the promotion transaction.");
        }
        context.Bindings.Add(binding);
        try
        {
            await context.SaveChangesAsync();
            context.Entry(binding).State = EntityState.Detached;
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            context.Entry(binding).State = EntityState.Detached;
            return false;
        }
    }

    static bool IsUniqueConflict(DbUpdateException exception) => exception.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqlException sql => sql.Number is 2601 or 2627,
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        _ => false
    };
}
