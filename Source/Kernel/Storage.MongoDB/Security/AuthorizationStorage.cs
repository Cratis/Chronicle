// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// MongoDB implementation of <see cref="IAuthorizationStorage"/>.
/// </summary>
/// <param name="database">MongoDB database.</param>
public class AuthorizationStorage(IMongoDatabase database) : IAuthorizationStorage
{
    const string CollectionName = WellKnownCollectionNames.Authorizations;
    readonly IMongoCollection<Authorization> _collection = database.GetCollection<Authorization>(CollectionName);

    /// <inheritdoc/>
    public async Task<Authorization?> GetById(string id, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Id == id, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task Create(Authorization authorization, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(authorization, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Update(Authorization authorization, CancellationToken cancellationToken = default) =>
        _collection.ReplaceOneAsync(_ => _.Id == authorization.Id, authorization, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Delete(string id, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(_ => _.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        var findOptions = new FindOptions<Authorization>
        {
            Skip = offset,
            Limit = count
        };

        var cursor = await _collection.FindAsync(_ => true, findOptions, cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationId(string applicationId, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.ApplicationId == applicationId, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindBySubject(string subject, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Subject == subject, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationIdAndSubject(string applicationId, string subject, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.ApplicationId == applicationId && _.Subject == subject, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationIdSubjectAndStatus(string applicationId, string subject, string status, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.ApplicationId == applicationId && _.Subject == subject && _.Status == status, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteManyAsync(
            _ => _.CreationDate != null && _.CreationDate < threshold,
            cancellationToken);

        return result.DeletedCount;
    }
}
