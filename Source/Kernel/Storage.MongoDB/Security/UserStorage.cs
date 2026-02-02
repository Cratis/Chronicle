// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Security;
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
    static readonly Collation _collation = new("en", caseLevel: false, strength: CollationStrength.Primary);
    static readonly FindOptions<User> _findOptions = new() { Collation = _collation };

    /// <inheritdoc/>
    public ISubject<IEnumerable<User>> ObserveAll() =>
        new TransformingSubject<IEnumerable<User>, IEnumerable<User>>(
            GetCollection().Observe(),
            users => users);

    /// <inheritdoc/>
    public async Task<User?> GetById(UserId id)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Id == id);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<User?> GetByUsername(Username username)
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Username == username, _findOptions);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmail(UserEmail email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var collection = GetCollection();
        using var cursor = await collection.FindAsync(u => u.Email == email, _findOptions);
        return await cursor.FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task Create(User user)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(user);
    }

    /// <inheritdoc/>
    public async Task Update(User user)
    {
        var collection = GetCollection();
        await collection.ReplaceOneAsync(
            u => u.Id == user.Id,
            user,
            new ReplaceOptions { IsUpsert = false });
    }

    /// <inheritdoc/>
    public async Task Delete(UserId id)
    {
        var collection = GetCollection();
        await collection.DeleteOneAsync(u => u.Id == id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        var collection = GetCollection();
        using var cursor = await collection.FindAsync(_ => true);
        return await cursor.ToListAsync();
    }

    IMongoCollection<User> GetCollection()
    {
        return database.GetCollection<User>(CollectionName);
    }
}
