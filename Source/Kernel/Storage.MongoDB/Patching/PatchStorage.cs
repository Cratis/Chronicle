// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Storage.Patching;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
public class PatchStorage(IDatabase database) : IPatchStorage
{
    const string CollectionName = "patches";

    IMongoCollection<Patch> Collection => database.GetCollection<Patch>(CollectionName);

    /// <inheritdoc/>
    public async Task<IEnumerable<Patch>> GetAll()
    {
        var cursor = await Collection.FindAsync(FilterDefinition<Patch>.Empty);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public Task Save(Patch patch) =>
        Collection.ReplaceOneAsync(
            p => p.Name == patch.Name,
            patch,
            new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public async Task<bool> Has(string patchName)
    {
        var count = await Collection.CountDocumentsAsync(p => p.Name == patchName);
        return count > 0;
    }

    /// <inheritdoc/>
    public Task Remove(string patchName) =>
        Collection.DeleteOneAsync(p => p.Name == patchName);
}
