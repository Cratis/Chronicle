// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Security;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// Represents an implementation of <see cref="IUserStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IDatabase"/> to use for accessing database.</param>
public class UserStorage(IDatabase database) : IUserStorage
{
    const string CollectionName = WellKnownCollectionNames.Users;

    /// <inheritdoc/>
    public ISubject<IEnumerable<ChronicleUser>> ObserveAll() =>
        new TransformingSubject<IEnumerable<ChronicleUser>, IEnumerable<ChronicleUser>>(
            GetCollection().Observe(),
            users => users);

    /// <inheritdoc/>
    public async Task<ChronicleUser?> GetById(string id)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Id == id);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ChronicleUser?> GetByUsername(string username)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Username == username);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ChronicleUser?> GetByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Email == email);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task Create(ChronicleUser user)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(user);
    }

    /// <inheritdoc/>
    public async Task Update(ChronicleUser user)
    {
        var collection = GetCollection();
        await collection.ReplaceOneAsync(
            u => u.Id == user.Id,
            user,
            new ReplaceOptions { IsUpsert = false });
    }

    /// <inheritdoc/>
    public async Task Delete(string id)
    {
        var collection = GetCollection();
        await collection.DeleteOneAsync(u => u.Id == id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChronicleUser>> GetAll()
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(_ => true);
        return await cursor.ToListAsync();
    }

    IMongoCollection<ChronicleUser> GetCollection()
    {
        return database.GetCollection<ChronicleUser>(CollectionName);
    }
}
