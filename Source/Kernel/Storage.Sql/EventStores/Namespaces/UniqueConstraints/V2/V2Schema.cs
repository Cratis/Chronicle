// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Separates schema contexts from operation transactions. The local gate only reduces duplicate DDL;
/// TableMigrator and the database schema lock coordinate independent processes.
/// </summary>
/// <param name="createOptions">Creates independent connection configurations.</param>
sealed class V2Schema(Func<DbContextOptions> createOptions)
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    readonly ITableMigrator<V2DbContext> _migrator = new TableMigrator<V2DbContext>(NullLogger<TableMigrator<V2DbContext>>.Instance);

    internal V2DbContext Context(string? table = null, UniqueConstraintMode mode = UniqueConstraintMode.PerEventSource) => new(createOptions(), table, mode);

    internal static Task<bool> Exists(V2DbContext context, string table) => TableMigrator<V2DbContext>.TableExists(context, table);

    internal Task EnsureMetadata(CancellationToken cancellationToken) => Locked(async context =>
    {
        await _migrator.EnsureTableMigrated(V2Identity.Authorities, context, (schema, table) => V2MetadataMigration.CreateAuthority(_migrator, schema, table));
        await _migrator.EnsureTableMigrated(V2Identity.Bindings, context, (schema, table) => V2MetadataMigration.CreateBindings(_migrator, schema, table));
    }, cancellationToken);

    internal async Task<string?> Resolve(V2Binding expected, bool create, CancellationToken cancellationToken)
    {
        if (create)
        {
            await Locked(context => EnsureClaim(context, expected, cancellationToken), cancellationToken);
            return expected.Table;
        }

        await using var context = Context();
        if (!await Exists(context, V2Identity.Bindings)) return null;
        var stored = await context.Bindings.AsNoTracking().SingleOrDefaultAsync(row => row.Id == expected.Id, cancellationToken);
        if (stored is null) return null;
        Verify(stored, expected);
        if (!await Exists(context, stored.Table)) throw new UniqueConstraintV2SchemaMismatch();
        return stored.Table;
    }

    internal static void Verify(V2Binding actual, V2Binding expected)
    {
        if (actual != expected) throw new UniqueConstraintV2SchemaMismatch();
    }

    async Task EnsureClaim(V2DbContext context, V2Binding expected, CancellationToken cancellationToken)
    {
        var stored = await context.Bindings.AsNoTracking().SingleOrDefaultAsync(row => row.Id == expected.Id, cancellationToken);
        if (stored is not null)
        {
            Verify(stored, expected);
            if (!await Exists(context, stored.Table)) throw new UniqueConstraintV2SchemaMismatch();
            return;
        }

        if (await Exists(context, expected.Table) || await context.Bindings.AnyAsync(row => row.Table == expected.Table, cancellationToken))
        {
            throw new UniqueConstraintV2SchemaMismatch();
        }

        // DDL and binding publication are atomic. Never adopt an unbound pre-existing table.
        await using var claims = Context(expected.Table, expected.Mode);
        await _migrator.EnsureTableMigrated(expected.Table, claims, (schema, _) => V2ClaimMigration.Create(_migrator, schema, expected));
        var published = await context.Bindings.AsNoTracking().SingleAsync(row => row.Id == expected.Id, cancellationToken);
        Verify(published, expected);
    }

    async Task Locked(Func<V2DbContext, Task> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = Context();
        var gate = _gates.GetOrAdd($"{context.Database.ProviderName}|{context.Database.GetConnectionString()}", _ => new(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await DatabaseSchemaLock.Run(context, async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await operation(context);
            });
        }
        finally
        {
            gate.Release();
        }
    }
}
