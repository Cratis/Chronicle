// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Constraints;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

public partial class Database
{
    static readonly string[] _constraintDiscoveryColumns = ["Value", "EventSourceId", "SequenceNumber"];

    /// <summary>
    /// Prepares registered and catalog-discovered claim tables, including existing scoped names.
    /// </summary>
    /// <param name="identity">The namespace identity.</param>
    /// <param name="registry">The stored constraint definitions.</param>
    /// <param name="sequences">The known sequence names.</param>
    /// <param name="tables">The catalog-discovered tables and columns.</param>
    /// <param name="verify">Whether to inspect without executing DDL.</param>
    /// <returns>The preparation task.</returns>
    internal async Task PrepareConstraintTables(DatabaseIdentity identity, EventStoreDbContext registry, IEnumerable<string> sequences, IReadOnlyDictionary<string, string[]> tables, bool verify)
    {
        using var inspection = verify ? DynamicSchemaVerification.Begin() : null;
        var target = await Target(identity, ConnectionPurpose.Schema, verify);
        if (!verify) uniqueConstraintMigrator.ClearMigrationCache(target.ConnectionString);
        var definitions = (await registry.Constraints.AsNoTracking().ToListAsync()).GroupBy(row => row.Name)
            .Select(group => group.OrderByDescending(row => row.Version).First().ToKernel()).OfType<UniqueConstraintDefinition>().ToArray();
        var claims = new HashSet<string>(StringComparer.Ordinal);
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            foreach (var sequence in sequences.Where(sequence => definition.AppliesTo(sequence)))
            {
                // Scoped values are only known after traffic has supplied the scope key. Their existing
                // physical names are discovered below rather than invented by the provisioner.
                if (definition.Scope is not null) continue;
                var name = $"{sequence}_{definition.Name}";
                if (definition.Mode == UniqueConstraintMode.PerValue) values.Add($"{name}_constraint_values");
                else claims.Add($"{name}_constraint");
            }
        }
        foreach (var table in tables.Where(table => table.Value.Contains("Value", StringComparer.Ordinal) && table.Value.Contains("EventSourceId", StringComparer.Ordinal) && table.Value.Contains("SequenceNumber", StringComparer.Ordinal)))
        {
            // Near PostgreSQL's byte limit, truncation can turn a values suffix into a claims suffix.
            // Only the catalog-primary-key classification below may admit those physical names.
            if (target.ConnectionString.GetDatabaseType() == Cratis.Arc.EntityFrameworkCore.DatabaseType.PostgreSql && Encoding.UTF8.GetByteCount(table.Key) >= 60) continue;
            if (table.Key.EndsWith("_constraint_values", StringComparison.Ordinal)) values.Add(table.Key);
            else if (table.Key.EndsWith("_constraint", StringComparison.Ordinal)) claims.Add(table.Key);
        }
        var discovered = await DiscoverTruncatedConstraintTables(target, sequences, tables, definitions);
        claims.UnionWith(discovered.Claims);
        values.UnionWith(discovered.Values);
        foreach (var name in claims)
        {
            await using var context = new UniqueConstraintDbContext(BuildOptions<UniqueConstraintDbContext>(target), name, uniqueConstraintMigrator);
            await context.EnsureTableExists();
        }
        foreach (var name in values)
        {
            await using var context = new UniqueConstraintValuesDbContext(BuildOptions<UniqueConstraintValuesDbContext>(target), name, uniqueConstraintMigrator);
            await context.EnsureTableExists();
        }
    }

    async Task<(HashSet<string> Claims, HashSet<string> Values)> DiscoverTruncatedConstraintTables(SqlTarget target, IEnumerable<string> sequences, IReadOnlyDictionary<string, string[]> tables, UniqueConstraintDefinition[] definitions)
    {
        var claims = new HashSet<string>(StringComparer.Ordinal);
        var values = new HashSet<string>(StringComparer.Ordinal);
        await using var catalog = new NamespaceDbContext(BuildOptions<NamespaceDbContext>(target));
        if (!catalog.Database.IsNpgsql()) return (claims, values);
        foreach (var sequence in sequences)
        {
            var discovered = await UniqueConstraintTables.For(catalog, sequence);
            var registered = definitions.Where(definition => definition.AppliesTo(sequence)).ToArray();
            bool Matches(string name, UniqueConstraintMode mode) => tables.TryGetValue(name, out var columns)
                && _constraintDiscoveryColumns.All(column => columns.Contains(column, StringComparer.Ordinal))
                && registered.Any(definition => definition.Mode == mode && name.StartsWith($"{sequence}_{definition.Name}_", StringComparison.Ordinal));
            claims.UnionWith(discovered.PerEventSource.Where(name => Matches(name, UniqueConstraintMode.PerEventSource)));
            values.UnionWith(discovered.PerValue.Where(name => Matches(name, UniqueConstraintMode.PerValue)));
        }
        return (claims, values);
    }
}
