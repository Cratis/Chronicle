// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ClientCredentials;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.ClientCredentials;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IClientCredentialsStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientCredentialsStorage"/> class.
/// </remarks>
/// <param name="database">The MongoDB database.</param>
public class ClientCredentialsStorage(IMongoDatabase database) : IClientCredentialsStorage
{
    const string CollectionName = "clients";
    readonly IMongoCollection<ChronicleClient> _collection = database.GetCollection<ChronicleClient>(CollectionName);

    /// <inheritdoc/>
    public async Task<ChronicleClient?> GetById(string id)
    {
        var filter = Builders<ChronicleClient>.Filter.Eq(c => c.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ChronicleClient?> GetByClientId(string clientId)
    {
        var filter = Builders<ChronicleClient>.Filter.Eq(c => c.ClientId, clientId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task Create(ChronicleClient client)
    {
        await _collection.InsertOneAsync(client);
    }

    /// <inheritdoc/>
    public async Task Update(ChronicleClient client)
    {
        var filter = Builders<ChronicleClient>.Filter.Eq(c => c.Id, client.Id);
        await _collection.ReplaceOneAsync(filter, client);
    }

    /// <inheritdoc/>
    public async Task Delete(string id)
    {
        var filter = Builders<ChronicleClient>.Filter.Eq(c => c.Id, id);
        await _collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChronicleClient>> GetAll()
    {
        return await _collection.Find(Builders<ChronicleClient>.Filter.Empty).ToListAsync();
    }
}
