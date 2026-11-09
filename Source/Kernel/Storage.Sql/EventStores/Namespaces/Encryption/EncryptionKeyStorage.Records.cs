// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

public partial class EncryptionKeyStorage : IEncryptionKeyFenceRecords
{
    /// <inheritdoc/>
    public async Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
        return observed.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ??
            await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId);
    }

    /// <inheritdoc/>
    public async Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            var observed = EncryptionKeyFenceWrites.Read(identifier, row);
            var next = (await EncryptionKeyRequestArchive.Prepare(this, eventStore, eventStoreNamespace, identifier, observed, retain)).WithStateSeal();
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            scope.DbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
            if (await EncryptionKeyFenceWrites.CompareExchange(scope.DbContext, identifier, row, next) == 1) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<long?> CountLegacyFences(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        scope.DbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
        return await scope.DbContext.EncryptionKeyErasures.LongCountAsync(_ => _.StateSeal == null);
    }

    /// <inheritdoc/>
    public async Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        scope.DbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
        var row = await scope.DbContext.EncryptionKeyFenceRecords.AsNoTracking().SingleOrDefaultAsync(_ => _.Id == id);
        if (row is null) return null;
        if (row.Seal != EncryptionKeyFenceState.Seal(row.Value)) throw new InvalidOperationException("Invalid encryption key metadata seal.");
        return row.Value;
    }

    /// <inheritdoc/>
    public async Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var context = scope.DbContext;
        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
        string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
        var seal = EncryptionKeyFenceState.Seal(value);
        try
        {
            await EncryptionKeyFenceWrites.Write(null, () => EncryptionKeyFenceWrites.Execute(context, $"INSERT INTO {N("EncryptionKeyFenceRecords")} ({N("Id")}, {N("Value")}, {N("Seal")}) VALUES ({{0}}, {{1}}, {{2}})", id, value, seal));
        }
        catch (DbException exception) when (EncryptionKeyFenceWrites.IsDuplicate(exception))
        {
            if (replace)
            {
                await EncryptionKeyFenceWrites.Write(null, () => EncryptionKeyFenceWrites.Execute(context, $"UPDATE {N("EncryptionKeyFenceRecords")} SET {N("Value")} = {{1}}, {N("Seal")} = {{2}} WHERE {N("Id")} = {{0}}", id, value, seal));
            }
        }
    }
}
