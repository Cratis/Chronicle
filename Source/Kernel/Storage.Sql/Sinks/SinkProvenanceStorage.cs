// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Stores durable facts in one physical database, independent of configuration aliases.
/// </summary>
/// <param name="database">The resolved sink database.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="containerName">The ordinary destination, if any.</param>
public class SinkProvenanceStorage(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelContainerName? containerName = null) : ISinkProvenanceStorage
{
    const string Marker = "protected";

    /// <summary>
    /// Gets the actual physical placement captured by cold metadata admission.
    /// </summary>
    internal SinkProvenanceTarget? Target { get; private set; }

    /// <inheritdoc/>
    public async Task<SinkProvenanceState> ReadState()
    {
        await using var marker = await Open();
        var marked = await ContainsMarker(marker.DbContext);
        await using var inventory = await OpenInventory();
        var context = inventory.DbContext;
        var state = await ReadInventory(context);
        if (state is null)
        {
            if (SinkCycleReceiptSchema.IsVerify(context)) throw Unavailable();
            context.Entries.Add(new DynamicReadModelEntity
            {
                ["Id"] = "inventory",
                ["Version"] = 1,
                ["Restricted"] = marked,
                ["DatabaseId"] = Guid.NewGuid().ToString("D"),
                ["InventoryVersion"] = 0
            });
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await ReadInventory(context) is null) throw;
            }

            state = await ReadInventory(context) ?? throw Unavailable();
        }

        return (state with { Restricted = state.Restricted || marked }).Validate();
    }

    /// <inheritdoc/>
    public async Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence)
    {
        var state = await ReadState();
        if (state.IsComplete) return state;
        await using var scope = await OpenInventory();
        var context = scope.DbContext;
        var type = context.Database.GetDatabaseType();
        var complete = $"{SinkProvenanceSchema.Quote(type, "InventoryVersion")} = {{0}}";
        if (restrictedEvidence || state.Restricted) complete += $", {SinkProvenanceSchema.Quote(type, "Restricted")} = {{1}}";
        var sql = $"UPDATE {SinkProvenanceSchema.Qualified(context, SinkProvenanceNames.Inventory)} SET {complete} WHERE {SinkProvenanceSchema.Quote(type, "Id")} = {{2}} AND {SinkProvenanceSchema.Quote(type, "Version")} = 1 AND {SinkProvenanceSchema.Quote(type, "InventoryVersion")} = 0";
        await context.Database.ExecuteSqlRawAsync(sql, SinkProvenanceState.CurrentInventoryVersion, true, "inventory");
        var committed = await ReadState();
        if (!committed.IsComplete) throw Unavailable();
        return committed;
    }

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined() => (await ReadState()).Restricted;

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        await ReadState();
        await using var scope = await Open();
        var context = scope.DbContext;
        if (!await ContainsMarker(context))
        {
            context.Entries.Add(new DynamicReadModelEntity { ["Id"] = Marker, ["Version"] = 1, ["Restricted"] = true });
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (!await ContainsMarker(context)) throw;
            }
        }

        await using var inventory = await OpenInventory();
        var stateContext = inventory.DbContext;
        var type = stateContext.Database.GetDatabaseType();
#pragma warning disable EF1002 // Identifiers are provider-delimited by Qualified/Quote; every stored value remains parameterized.
        await stateContext.Database.ExecuteSqlRawAsync(
            $"UPDATE {SinkProvenanceSchema.Qualified(stateContext, SinkProvenanceNames.Inventory)} SET {SinkProvenanceSchema.Quote(type, "Restricted")} = {{0}} WHERE {SinkProvenanceSchema.Quote(type, "Id")} = {{1}}",
            true,
            "inventory");
#pragma warning restore EF1002
        if (!(await ReadState()).Restricted) throw Unavailable();
    }

    /// <summary>
    /// Validates destination reservation in the actual database collation.
    /// </summary>
    /// <returns>The destination validation task.</returns>
    internal async Task EnsureDestinationAvailable()
    {
        await using var scope = await Open();
    }

    static async Task<bool> ContainsMarker(ReadModelDbContext context)
    {
        var rows = await context.Entries.FromSqlRaw(SinkProvenanceSchema.Select(context)).AsNoTracking().Take(2).ToListAsync();
        if (rows.Count == 0) return false;
        if (rows.Count != 1 || !string.Equals(rows[0]["Id"] as string, Marker, StringComparison.Ordinal) || rows[0]["Version"] is not 1 || rows[0]["Restricted"] is not true) throw Unavailable();
        return true;
    }

    static async Task<SinkProvenanceState?> ReadInventory(ReadModelDbContext context)
    {
        var rows = await context.Entries.FromSqlRaw(SinkProvenanceSchema.Select(context)).AsNoTracking().Take(2).ToListAsync();
        if (rows.Count == 0) return null;
        if (rows.Count != 1) throw Unavailable();
        return ParseInventory(rows[0]);
    }

    static SinkProvenanceState ParseInventory(DynamicReadModelEntity row)
    {
        if (!string.Equals(row["Id"] as string, "inventory", StringComparison.Ordinal) || row["Version"] is not 1 ||
            row["Restricted"] is not bool restricted || row["InventoryVersion"] is not int version ||
            row["DatabaseId"] is not string id || !Guid.TryParseExact(id, "D", out var databaseId))
        {
            throw Unavailable();
        }

        return new SinkProvenanceState(new(databaseId), version, restricted).Validate();
    }

    static InvalidOperationException Unavailable() => new("Read-model storage is not available.");

    Task<DbContextScope<ReadModelDbContext>> OpenInventory() => database.ReadModelTable(
        eventStore,
        @namespace,
        SinkProvenanceNames.Inventory,
        SinkProvenanceSchema.InventoryColumns,
        admittedTarget: Target);

    async Task<DbContextScope<ReadModelDbContext>> Open()
    {
        if (containerName is not null) SinkProvenanceNames.EnsureNotReserved(containerName);
        var scope = await SinkProvenanceSchema.Open(database, eventStore, @namespace, Target);
        try
        {
            var actual = SinkProvenanceTarget.For(scope.DbContext);
            Target?.Ensure(scope.DbContext);
            Target = actual;
            await SinkProvenanceSchema.EnsureDestinationAvailable(scope.DbContext, containerName);
            return scope;
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }
}
