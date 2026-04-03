// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents an implementation of <see cref="IDataProtectionKeyStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class DataProtectionKeyStorage(IDatabase database) : IDataProtectionKeyStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<DataProtectionKey>> GetAll()
    {
        await using var scope = await database.Cluster();
        var entities = await scope.DbContext.DataProtectionKeys.ToListAsync();
        return entities.Select(e => new DataProtectionKey(e.Id, e.FriendlyName, e.Xml, e.Created)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Store(DataProtectionKey key)
    {
        await using var scope = await database.Cluster();
        var entity = new DataProtectionKeyEntity
        {
            Id = key.Id,
            FriendlyName = key.FriendlyName,
            Xml = key.Xml,
            Created = key.Created,
        };
        await scope.DbContext.DataProtectionKeys.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
