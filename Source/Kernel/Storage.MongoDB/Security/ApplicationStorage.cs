// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// MongoDB implementation of <see cref="IApplicationStorage"/>.
/// </summary>
/// <param name="database">MongoDB database.</param>
public class ApplicationStorage(IDatabase database) : IApplicationStorage
{
    const string CollectionName = WellKnownCollectionNames.Applications;
    readonly IMongoCollection<Application> _collection = database.GetCollection<Application>(CollectionName);

    /// <inheritdoc/>
    public async Task<Application?> GetById(ApplicationId id, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.Id == id, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Application?> GetByClientId(ClientId clientId, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.ClientId == clientId, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task Create(Application application, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(application, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Update(Application application, CancellationToken cancellationToken = default) =>
        _collection.ReplaceOneAsync(_ => _.Id == application.Id, application, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Delete(ApplicationId id, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(_ => _.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> GetAll(CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => true, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        var findOptions = new FindOptions<Application>
        {
            Skip = offset,
            Limit = count
        };

        var cursor = await _collection.FindAsync(_ => true, findOptions, cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> FindByRedirectUri(string redirectUri, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.RedirectUris.Contains(redirectUri), cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> FindByPostLogoutRedirectUri(string postLogoutRedirectUri, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(_ => _.PostLogoutRedirectUris.Contains(postLogoutRedirectUri), cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }
}
