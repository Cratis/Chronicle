// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Stores the event store's unique-constraint encoding key as one row, inserted atomically and never updated.
/// </summary>
/// <param name="open">Opens a store-scoped context.</param>
public sealed class UniqueConstraintEncodingStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : IUniqueConstraintEncodingStorage
{
    const int SingletonId = 1;

    /// <inheritdoc/>
    public async Task<UniqueConstraintEncodingKey?> GetKey()
    {
        await using var scope = await open();
        var row = await scope.DbContext.UniqueConstraintEncodingKey.AsNoTracking().SingleOrDefaultAsync(_ => _.Id == SingletonId);
        return row is null ? null : Decode(row);
    }

    /// <inheritdoc/>
    public async Task<UniqueConstraintEncodingKey> CreateKeyIfAbsent(UniqueConstraintEncodingKey candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var keyId = UniqueConstraintStoreKeyIdBytes.From(candidate.KeyId);
        var material = candidate.Material.CopyBytes();
        try
        {
            await using var scope = await open();
            scope.DbContext.UniqueConstraintEncodingKey.Add(new UniqueConstraintEncodingKeyRow { Id = SingletonId, KeyId = keyId, Material = material });
            try
            {
                await scope.DbContext.SaveChangesAsync();
                return candidate;
            }
            catch (DbUpdateException error) when (error.IsUniqueViolation())
            {
                // Another writer won. Read the winner from a fresh scope below.
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }

        return await GetKey() ?? throw new InvalidOperationException("The unique-constraint encoding key could not be read after a concurrent insert.");
    }

    static UniqueConstraintEncodingKey Decode(UniqueConstraintEncodingKeyRow row)
    {
        try
        {
            return new(UniqueConstraintStoreKeyIdBytes.To(row.KeyId), new UniqueConstraintStoreKey(row.Material));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(row.Material);
        }
    }
}
