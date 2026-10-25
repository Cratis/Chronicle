// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql;

public partial class Database
{
    /// <summary>
    /// Reuses registered schema metadata for primary and existing replay/backup read-model tables.
    /// </summary>
    /// <param name="identity">The namespace identity.</param>
    /// <param name="registry">The stored read-model definitions.</param>
    /// <param name="verify">Whether to inspect without executing DDL.</param>
    /// <returns>The preparation task.</returns>
    internal async Task PrepareRegisteredReadModels(DatabaseIdentity identity, EventStoreDbContext registry, bool verify)
    {
        using var inspection = verify ? DynamicSchemaVerification.Begin() : null;
        using var policy = verify ? DynamicSchemaGuard.Use(DynamicSchemaPolicy.Refuse) : null;
        var target = await Target(identity with { Kind = DatabaseKind.ReadModels }, ConnectionPurpose.Schema, verify);
        if (!verify) readModelMigrator.ClearMigrationCache(target.ConnectionString);
        var configuration = serviceProvider.GetService<IOptions<ChronicleOptions>>()?.Value.Sql ?? new Configuration.Sql();

        // SQL generations are not supported. Future generation provisioning belongs alongside this manifest.
        var definitions = await registry.ReadModels.AsNoTracking().ToListAsync();
        foreach (var row in definitions.Where(row => row.SinkType == Concepts.Sinks.WellKnownSinkTypes.SQL))
        {
            var definition = row.ToKernel();
            var schema = definition.GetSchemaForLatestGeneration();
            var columns = ProjectedColumns.ForSchema(schema, definition.Indexes);
            var primary = definition.ContainerName.Value;

            // The context is disposed by the await using declaration; CA2000 misreads it, as in Database.cs.
#pragma warning disable CA2000
            await using var context = new ReadModelDbContext(BuildOptions<ReadModelDbContext>(target), primary, columns, readModelMigrator);
#pragma warning restore CA2000
            await context.EnsureReplayPromotions();
            var tables = await SchemaTables.Read(context);
            var backups = tables.ContainsKey(WellKnownTableNames.ReplayPromotions)
                ? await context.ReplayPromotions.AsNoTracking().Where(marker => marker.ContainerName == primary).Select(marker => marker.BackupTableName).ToListAsync()
                : [];
            var type = context.Database.GetDatabaseType();
            var replay = PrimaryKeyNames.TableIdentifier(type, $"replay-{primary}");
            foreach (var name in tables.Keys.Where(name => name == replay || backups.Contains(name)).Append(primary).Distinct(StringComparer.Ordinal))
            {
                // The await using declaration owns disposal; CA2000 misreads this path, as above.
#pragma warning disable CA2000
                await using var table = new ReadModelDbContext(BuildOptions<ReadModelDbContext>(target), name, columns, readModelMigrator);
#pragma warning restore CA2000
                await table.EnsureTableExists();
                if (!verify) await RequireInternalReadModelIndexes(table);
                foreach (var index in definition.Indexes)
                {
                    var plan = ReadModelIndexPlan.Resolve(index, schema, columns, type);
                    if (plan is null) continue;
                    if (verify)
                    {
                        // A legacy SQLite decimal declaration is implemented by its required ordering-shadow index,
                        // not by a declared-name index (ProjectedColumnIndexes removes that obsolete TEXT index).
                        var exists = type == DatabaseType.Sqlite && plan.IsLegacy && SqliteDecimalSortKey.IsDecimal(plan.Targets[0].Column)
                            ? ShadowColumns.IndexedFor(table).Contains(SqliteDecimalSortKey.ColumnNameFor(plan.Targets[0].Column.Name), StringComparer.Ordinal) && !await ShadowColumnIndexes.NeedsReconciliation(table)
                            : await DynamicIndexShape.Exists(table, name, plan.Name(type, name));

                        // Other declared indexes are checked by generated name only, not full shape.
                        if (!exists) DynamicSchemaVerification.Report($"declared index '{name}.{plan.Name(type, name)}' is missing");
                        continue;
                    }
                    if (await SqlServerIndexKeyBudget.RejectionReason(table, name, plan) is not null) continue;
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.ShadowIndexDdlTimeoutSeconds));
                    await ProjectedColumnIndexes.Ensure(table, name, plan, new ReadModelIndexOperations.Execution(false, configuration.ShadowIndexDdlTimeoutSeconds, CancellationToken: deadline.Token));
                }
            }
        }
    }

    static async Task RequireInternalReadModelIndexes(ReadModelDbContext context)
    {
        var pending = new List<string>();
        if (await ShadowColumnIndexes.NeedsReconciliation(context)) pending.Add($"table '{context.TableName}' has missing or invalid shadow indexes");
        if (await InitializedMarkerIndex.NeedsIndex(context)) pending.Add($"table '{context.TableName}' has a missing or invalid initialized-marker index");
        if (pending.Count > 0) throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, context.TableName, pending);
    }
}
