// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents durable, database-local provenance, independent of configuration aliases and process caches.
/// </summary>
/// <param name="database">The same resolved database used by the sink.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="containerName">The ordinary destination to validate, if any.</param>
public class SinkProvenanceStorage(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelContainerName? containerName = null) : ISinkProvenanceStorage
{
    const string Marker = "protected";

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined()
    {
        await using var scope = await Open();
        return await ContainsMarker(scope.DbContext);
    }

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        await using var scope = await Open();
        var context = scope.DbContext;
        if (await ContainsMarker(context)) return;
        context.Entries.Add(new DynamicReadModelEntity { ["Id"] = Marker, ["Version"] = 1, ["Restricted"] = true });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A competing writer is successful only when its marker is actually visible on the primary.
            if (!await ContainsMarker(context)) throw;
        }

        if (!await ContainsMarker(context)) throw new InvalidOperationException("Read-model storage is not available.");
    }

    internal async Task EnsureDestinationAvailable()
    {
        await using var scope = await Open();
    }

    async Task<DbContextScope<ReadModelDbContext>> Open()
    {
        if (containerName is not null) SinkProvenanceNames.EnsureNotReserved(containerName);
        var scope = await SinkProvenanceSchema.Open(database, eventStore, @namespace);
        try
        {
            await SinkProvenanceSchema.EnsureDestinationAvailable(scope.DbContext, containerName);
            return scope;
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    static async Task<bool> ContainsMarker(ReadModelDbContext context)
    {
        var rows = await context.Entries.FromSqlRaw(SinkProvenanceSchema.Select(context.Database.GetDatabaseType())).AsNoTracking().Take(2).ToListAsync();
        if (rows.Count == 0) return false;
        if (rows.Count != 1 || !IsMarker(rows[0]))
        {
            throw new InvalidOperationException("Read-model storage is not available.");
        }

        return true;
    }

    static bool IsMarker(DynamicReadModelEntity row) => (row["Id"], row["Version"], row["Restricted"]) is (Marker, 1, true);
}
