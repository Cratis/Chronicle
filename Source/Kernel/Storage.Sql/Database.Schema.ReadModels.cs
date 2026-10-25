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
                await using var table = new ReadModelDbContext(BuildOptions<ReadModelDbContext>(target), name, columns, readModelMigrator);
                await table.EnsureTableExists();
                foreach (var index in definition.Indexes)
                {
                    var plan = ReadModelIndexPlan.Resolve(index, schema, columns, type);
                    if (plan is null) continue;
                    if (verify)
                    {
                        // Declared indexes are checked by generated name only, not keys, ordering, filters, or expression SQL.
                        if (!await DynamicIndexShape.Exists(table, name, plan.Name(type, name))) DynamicSchemaVerification.Report($"declared index '{name}.{plan.Name(type, name)}' is missing");
                        continue;
                    }
                    if (await SqlServerIndexKeyBudget.RejectionReason(table, name, plan) is not null) continue;
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.ShadowIndexDdlTimeoutSeconds));
                    await ProjectedColumnIndexes.Ensure(table, name, plan, new ReadModelIndexOperations.Execution(false, configuration.ShadowIndexDdlTimeoutSeconds, CancellationToken: deadline.Token));
                }
            }
        }
    }
}
