// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Constraints;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.given;

public static class RegisteredDynamicSchema
{
    public const string Primary = "registered_model";
    public const string Replay = "replay-registered_model";
    public const string Backup = "backup-registered_model";
    public const string ScopedClaim = "event-log_registered_scope_constraint";
    public const string ScopedValues = "event-log_registered_scope_constraint_values";
    public const string RegisteredClaim = "event-log_registered_constraint";
    public const string RegisteredValueIndex = "IX_event-log_registered_constraint_Value";
    public static readonly string[] ReadModelTables = [Primary, Replay, Backup];

    public static ReadModelDefinition Definition => new()
    {
        Id = "registered-model",
        Name = Primary,
        SinkType = Concepts.Sinks.WellKnownSinkTypes.SQL,
        IndexesJson = "[\"name\"]",
        Schemas = new Dictionary<uint, string>
        {
            [1] = """{"type":"object","properties":{"name":{"type":"string"},"score":{"type":"integer"}}}"""
        }
    };

    public static IReadOnlyList<ProjectedColumn> Columns => ProjectedColumns.ForSchema(Definition.ToKernel().GetSchemaForLatestGeneration(), Definition.ToKernel().Indexes);

    public static async Task Register(Database database, EventStoreName store, EventStoreNamespaceName ns)
    {
        await using var registry = await database.EventStore(store);
        registry.DbContext.ReadModels.Add(Definition);
        registry.DbContext.EventStoreSubscriptions.Add(new() { Id = "publisher-subscription", SourceEventStore = "publisher" });
        registry.DbContext.Constraints.Add(new UniqueConstraintDefinition("registered", []).ToSql(1));
        registry.DbContext.Constraints.Add(new UniqueConstraintDefinition("registered_values", []) { Mode = UniqueConstraintMode.PerValue }.ToSql(1));
        await registry.DbContext.SaveChangesAsync();

        // Scoped physical names are only discoverable from the catalog, not from registration.
        await using (var claims = await database.UniqueConstraintTable(store, ns, ScopedClaim)) await claims.DbContext.EnsureTableExists();
        await using (var values = await database.UniqueConstraintValuesTable(store, ns, ScopedValues)) await values.DbContext.EnsureTableExists();
        foreach (var name in new[] { Replay, Backup })
        {
            await using var table = await database.ReadModelTable(store, ns, name, []);
            await table.DbContext.EnsureTableExists();
        }
        await using var backup = await database.ReadModelTable(store, ns, Backup, []);
        await backup.DbContext.EnsureReplayPromotions();
        backup.DbContext.ReplayPromotions.Add(new(ReplayPromotion.IdentifierFor(Primary, Backup), Primary, Backup, Backup));
        await backup.DbContext.SaveChangesAsync();
    }

    public static async Task<bool> AllObjectsExist(Database database, EventStoreName store, EventStoreNamespaceName ns)
    {
        using var inspection = DynamicSchemaVerification.Begin();
        foreach (var name in ReadModelTables)
        {
            await using var table = await database.ReadModelTable(store, ns, name, Columns);
            var tables = await SchemaTables.Read(table.DbContext);
            if (!tables.TryGetValue(name, out var columns) || Columns.Any(column => !columns.Contains(column.Name))) return false;
            var definition = Definition.ToKernel();
            var type = table.DbContext.Database.GetDatabaseType();
            var plan = ReadModelIndexPlan.Resolve(definition.Indexes.Single(), definition.GetSchemaForLatestGeneration(), Columns, type)!;
            if (!await DynamicIndexShape.Exists(table.DbContext, name, plan.Name(type, name))) return false;
            if (await InitializedMarkerIndex.NeedsIndex(table.DbContext)) return false;
            if (await ShadowColumnIndexes.NeedsReconciliation(table.DbContext)) return false;
        }
        await using var claims = await database.UniqueConstraintTable(store, ns, RegisteredClaim);
        if (!await DynamicIndexShape.IsUniqueValue(claims.DbContext, RegisteredClaim, RegisteredValueIndex)) return false;
        await using var values = await database.UniqueConstraintValuesTable(store, ns, "event-log_registered_values_constraint_values");
        if (!(await SchemaTables.Read(values.DbContext)).ContainsKey("event-log_registered_values_constraint_values")) return false;
        var tablesInNamespace = await SchemaTables.Read(claims.DbContext);
        return tablesInNamespace.ContainsKey(ScopedClaim) && tablesInNamespace.ContainsKey(ScopedValues)
            && tablesInNamespace.TryGetValue($"{EventSequenceId.InboxPrefix}publisher", out var inbox) && SchemaTables.IsSequence(inbox);
    }

    public static async Task BreakIndexes(Database database, EventStoreName store, EventStoreNamespaceName ns)
    {
        await using var model = await database.ReadModelTable(store, ns, Primary, Columns);
        var definition = Definition.ToKernel();
        var type = model.DbContext.Database.GetDatabaseType();
        var plan = ReadModelIndexPlan.Resolve(definition.Indexes.Single(), definition.GetSchemaForLatestGeneration(), Columns, type)!;
        await ReadModelIndexOperations.Drop(model.DbContext, Primary, plan.Name(type, Primary), online: false);
        await using var claims = await database.UniqueConstraintTable(store, ns, RegisteredClaim);
        var helper = claims.DbContext.GetService<ISqlGenerationHelper>();
        var name = helper.DelimitIdentifier(RegisteredValueIndex);
        var sql = type == DatabaseType.SqlServer
            ? $"DROP INDEX {name} ON {helper.DelimitIdentifier(RegisteredClaim)}" : $"DROP INDEX {name}";
#pragma warning disable EF1002 // Only fixture-owned identifiers delimited by ISqlGenerationHelper.
        await claims.DbContext.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }
}
