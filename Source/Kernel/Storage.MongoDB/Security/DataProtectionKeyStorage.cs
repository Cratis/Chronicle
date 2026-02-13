// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// MongoDB implementation of <see cref="IDataProtectionKeyStorage"/>.
/// </summary>
/// <param name="database">MongoDB database.</param>
public class DataProtectionKeyStorage(IDatabase database) : IDataProtectionKeyStorage
{
    const string CollectionName = WellKnownCollectionNames.DataProtectionKeys;
    readonly IMongoCollection<DataProtectionKey> _collection = database.GetCollection<DataProtectionKey>(CollectionName);

    /// <inheritdoc/>
    public async Task<IEnumerable<DataProtectionKey>> GetAll()
    {
        var cursor = await _collection.FindAsync(FilterDefinition<DataProtectionKey>.Empty);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public Task Store(DataProtectionKey key) =>
        _collection.InsertOneAsync(key);
}
