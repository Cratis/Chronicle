// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents an implementation of <see cref="IUserStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class UserStorage(IDatabase database) : IUserStorage, IDisposable
{
    readonly BehaviorSubject<IEnumerable<User>> _subject = new([]);

    /// <inheritdoc/>
    public ISubject<IEnumerable<User>> ObserveAll()
    {
        Task.Run(async () =>
        {
            var users = await GetAll();
            _subject.OnNext(users);
        });
        return _subject;
    }

    /// <inheritdoc/>
    public async Task<User?> GetById(UserId id)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Users.FindAsync(id.Value);
        return entity is null ? null : ToUser(entity);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByUsername(Username username)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Users.FirstOrDefaultAsync(u => u.Username == username.Value);
        return entity is null ? null : ToUser(entity);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmail(UserEmail email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Users.FirstOrDefaultAsync(u => u.Email == email.Value);
        return entity is null ? null : ToUser(entity);
    }

    /// <inheritdoc/>
    public async Task Create(User user)
    {
        await using var scope = await database.Cluster();
        scope.DbContext.Users.Add(ToEntity(user));
        await scope.DbContext.SaveChangesAsync();
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task Update(User user)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Users.Upsert(ToEntity(user));
        await scope.DbContext.SaveChangesAsync();
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task Delete(UserId id)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Users.FindAsync(id.Value);
        if (entity is not null)
        {
            scope.DbContext.Users.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
        }
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        await using var scope = await database.Cluster();
        var entities = await scope.DbContext.Users.ToListAsync();
        return entities.Select(ToUser).ToArray();
    }

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    static User ToUser(UserEntity entity) => new()
    {
        Id = (UserId)entity.Id,
        Username = (Username)entity.Username,
        Email = entity.Email is null ? null : (UserEmail)entity.Email,
        PasswordHash = entity.PasswordHash is null ? null : (UserPassword)entity.PasswordHash,
        SecurityStamp = entity.SecurityStamp is null ? null : (SecurityStamp)entity.SecurityStamp,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        LastModifiedAt = entity.LastModifiedAt,
        RequiresPasswordChange = entity.RequiresPasswordChange,
        HasLoggedIn = entity.HasLoggedIn,
    };

    static UserEntity ToEntity(User user) => new()
    {
        Id = user.Id.Value,
        Username = user.Username.Value,
        Email = user.Email?.Value,
        PasswordHash = user.PasswordHash?.Value,
        SecurityStamp = user.SecurityStamp?.Value,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastModifiedAt = user.LastModifiedAt,
        RequiresPasswordChange = user.RequiresPasswordChange,
        HasLoggedIn = user.HasLoggedIn,
    };

    async Task RefreshSubject()
    {
        var users = await GetAll();
        _subject.OnNext(users);
    }
}
