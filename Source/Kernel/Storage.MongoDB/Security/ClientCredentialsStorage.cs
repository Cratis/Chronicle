// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Security;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// Represents an implementation of <see cref="IClientCredentialsStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientCredentialsStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IDatabase"/> to use for accessing database.</param>
public class ClientCredentialsStorage(IDatabase database) : IClientCredentialsStorage
{
    const string CollectionName = WellKnownCollectionNames.Clients;

    /// <inheritdoc/>
    public ISubject<IEnumerable<ChronicleClient>> ObserveAll() =>
        new TransformingSubject<IEnumerable<ChronicleClient>, IEnumerable<ChronicleClient>>(
            GetCollection().Observe(),
            clients => clients);

    /// <inheritdoc/>
    public async Task<ChronicleClient?> GetById(string id)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(c => c.Id == id);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ChronicleClient?> GetByClientId(string clientId)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(c => c.ClientId == clientId);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChronicleClient>> GetAll()
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(_ => true);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task Create(ChronicleClient client)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(client);
    }

    /// <inheritdoc/>
    public async Task Update(ChronicleClient client)
    {
        var collection = GetCollection();
        await collection.ReplaceOneAsync(
            c => c.Id == client.Id,
            client,
            new ReplaceOptions { IsUpsert = false });
    }

    /// <inheritdoc/>
    public async Task Delete(string id)
    {
        var collection = GetCollection();
        await collection.DeleteOneAsync(c => c.Id == id);
    }

    IMongoCollection<ChronicleClient> GetCollection()
    {
        return database.GetCollection<ChronicleClient>(CollectionName);
    }
}
