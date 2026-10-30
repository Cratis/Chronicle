// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// Represents MongoDB storage of administrator privileges.
/// </summary>
/// <param name="database">The system database.</param>
public class AdministratorStorage(IDatabase database) : IAdministratorStorage
{
    readonly IMongoCollection<Administrator> _collection = database.GetCollection<Administrator>(WellKnownCollectionNames.Administrators);

    /// <inheritdoc/>
    public async Task<IEnumerable<Administrator>> GetAll()
    {
        using var cursor = await _collection.FindAsync(FilterDefinition<Administrator>.Empty);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public Task Save(Administrator administrator) => DuplicateKeyConvergence.UpsertWithRetry(() =>
        _collection.ReplaceOneAsync(row => row.Id == administrator.Id, administrator, new ReplaceOptions { IsUpsert = true }));

    /// <inheritdoc/>
    public async Task Delete(UserId userId) => await _collection.DeleteOneAsync(row => row.Id == userId);
}
