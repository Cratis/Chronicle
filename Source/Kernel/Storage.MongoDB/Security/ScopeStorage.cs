// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// MongoDB implementation of <see cref="IScopeStorage"/>.
/// </summary>
/// <param name="database">MongoDB database.</param>
public class ScopeStorage(IMongoDatabase database) : IScopeStorage
{
    const string CollectionName = WellKnownCollectionNames.Scopes;
    readonly IMongoCollection<Scope> _collection = database.GetCollection<Scope>(CollectionName);

    /// <inheritdoc/>
    public async Task<Scope?> GetById(string id, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Id == id, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Name == name, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task Create(Scope scope, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(scope, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Update(Scope scope, CancellationToken cancellationToken = default) =>
        _collection.ReplaceOneAsync(_ => _.Id == scope.Id, scope, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Delete(string id, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(_ => _.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        var findOptions = new FindOptions<Scope>
        {
            Skip = offset,
            Limit = count
        };

        var cursor = await _collection.FindAsync(_ => true, findOptions, cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> FindByResource(string resource, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Resources.Contains(resource), cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> FindByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var namesList = names.ToList();
        var cursor = await _collection.FindAsync(_ => _.Name != null && namesList.Contains(_.Name), cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }
}
