// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

public partial class EncryptionKeyStorage
{
    /// <inheritdoc/>
    public bool IsDurable => true;

    /// <inheritdoc/>
    public async Task<EncryptionKeyFenceState> ReadFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        try
        {
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            var context = scope.DbContext;
            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
            var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);
            var row = await context.EncryptionKeyExternalFences.AsNoTracking().SingleOrDefaultAsync(_ => _.Identifier == stored);
            if (row is null)
            {
                var empty = EncryptionKeyFenceState.Empty.Serialize();
                string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
                try
                {
                    var sql = $"INSERT INTO {N("EncryptionKeyExternalFences")} ({N("Identifier")}, {N("Version")}, {N("State")}, {N("Seal")}) VALUES ({{0}}, {{1}}, {{2}}, {{3}})";
                    await EncryptionKeyFenceWrites.Execute(context, sql, stored, 0m, empty, EncryptionKeyFenceState.Seal(empty));
                }
                catch (DbException exception) when (EncryptionKeyFenceWrites.IsDuplicate(exception))
                {
                    // Only a definite unique conflict is safe to reread.
                }

                row = await context.EncryptionKeyExternalFences.AsNoTracking().SingleOrDefaultAsync(_ => _.Identifier == stored);
            }

            if (row is null || decimal.Truncate(row.Version) != row.Version) throw new EncryptionKeyFenceUnavailable(identifier);
            return EncryptionKeyFenceState.Read(identifier, checked((ulong)row.Version), row.State, row.Seal);
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException or OverflowException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CompareExchangeFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next)
    {
        observed.EnsureMonotonic(identifier, next);
        try
        {
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            var context = scope.DbContext;
            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
            var state = next.Serialize();
            string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
            var sql = $"UPDATE {N("EncryptionKeyExternalFences")} SET {N("Version")} = {{0}}, {N("State")} = {{1}}, {N("Seal")} = {{2}} WHERE {N("Identifier")} = {{3}} AND {N("Version")} = {{4}} AND {N("Seal")} = {{5}}";
            var parameters = new object[]
            {
                (decimal)next.Erasure.Version, state, EncryptionKeyFenceState.Seal(state), EncryptionKeyIdentifierColumn.Value(context, identifier),
                (decimal)observed.Erasure.Version, EncryptionKeyFenceState.Seal(observed.Serialize())
            };
            return await EncryptionKeyFenceWrites.Execute(context, sql, parameters) == 1;
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }
}
