// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Stores complete lifecycle tombstones using a single atomic insert or revision-conditional update.
/// </summary>
/// <param name="open">Opens the event-store context, never the removable namespace context.</param>
public sealed class NamespaceLifecycleStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : INamespaceLifecycleStorage
{
    /// <inheritdoc/>
    public async Task<NamespaceLifecycle?> Get(EventStoreNamespaceName name)
    {
        var key = Key(name);
        await using var scope = await open();
        var row = await scope.DbContext.Set<NamespaceLifecycleRow>().AsNoTracking().SingleOrDefaultAsync(row => row.NameKey == key);
        if (row is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<NamespaceLifecycle>(row.Payload, JsonSerializerOptions.Default)
            ?? throw new InvalidOperationException("The namespace lifecycle record is invalid.");
    }

    /// <inheritdoc/>
    public async Task<bool> TryWrite(EventStoreNamespaceName name, long? expectedRevision, NamespaceLifecycle next)
    {
        if (next.Revision != (expectedRevision ?? 0) + 1 || !name.Value.Equals(next.Namespace.Value, StringComparison.OrdinalIgnoreCase)) return false;
        var key = Key(name);
        var payload = JsonSerializer.Serialize(next, JsonSerializerOptions.Default);
        await using var scope = await open();
        var rows = scope.DbContext.Set<NamespaceLifecycleRow>();
        if (expectedRevision is not null)
        {
            return await rows.Where(row => row.NameKey == key && row.Revision == expectedRevision.Value)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.Revision, next.Revision).SetProperty(row => row.Payload, payload)) == 1;
        }
        rows.Add(new() { NameKey = key, Revision = next.Revision, Payload = payload });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            return false;
        }
    }

    static byte[] Key(EventStoreNamespaceName name) => Encoding.UTF8.GetBytes(name.Value.ToUpperInvariant());
}
