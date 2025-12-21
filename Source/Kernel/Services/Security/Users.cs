// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Security.Cryptography;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage.Security;
using Cratis.Reactive;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IUsers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Users"/> class.
/// </remarks>
/// <param name="userStorage">The <see cref="IUserStorage"/> for working with users.</param>
internal sealed class Users(IUserStorage userStorage) : IUsers
{
    /// <inheritdoc/>
    public async Task Add(AddUser command)
    {
        var passwordHash = HashPassword(command.Password);
        
        var user = new ChronicleUser(
            command.UserId,
            command.Username,
            command.Email,
            passwordHash,
            Guid.NewGuid().ToString(),
            true,
            DateTimeOffset.UtcNow,
            null);

        await userStorage.Create(user);
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveUser command)
    {
        await userStorage.Delete(command.UserId);
    }

    /// <inheritdoc/>
    public async Task ChangePassword(ChangeUserPassword command)
    {
        var user = await userStorage.GetById(command.UserId);
        if (user is not null)
        {
            var passwordHash = HashPassword(command.Password);
            var updatedUser = user with
            {
                PasswordHash = passwordHash,
                SecurityStamp = Guid.NewGuid().ToString(),
                LastModifiedAt = DateTimeOffset.UtcNow
            };
            await userStorage.Update(updatedUser);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        var users = await userStorage.GetAll();
        return users.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<User>> ObserveAll(CallContext context = default) =>
        userStorage
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(users => users.Select(ToContract).ToArray());

    static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hashed);
    }

    static User ToContract(ChronicleUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastModifiedAt = user.LastModifiedAt
    };
}
