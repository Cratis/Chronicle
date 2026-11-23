// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
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
public class SinkProvenanceStorage(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace) : ISinkProvenanceStorage
{
    const string Marker = "protected";

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined()
    {
        await using var scope = await SinkProvenanceSchema.Open(database, eventStore, @namespace);
        return await ContainsMarker(scope.DbContext);
    }

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        await using var scope = await SinkProvenanceSchema.Open(database, eventStore, @namespace);
        var context = scope.DbContext;
        if (await ContainsMarker(context)) return;
        context.Entries.Add(new DynamicReadModelEntity { ["Id"] = Marker });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A competing writer is successful only when its marker is actually visible on the primary.
            if (!await ContainsMarker(context)) throw;
        }
    }

    static Task<bool> ContainsMarker(ReadModelDbContext context) =>
        context.Entries.AnyAsync(row => EF.Property<string>(row, "Id") == Marker);
}
