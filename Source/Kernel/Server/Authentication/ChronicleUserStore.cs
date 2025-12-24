// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents a user store for Chronicle users that implements ASP.NET Identity interfaces.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleUserStore"/> class.
/// </remarks>
/// <param name="userStorage">The user storage.</param>
public class ChronicleUserStore(IUserStorage userStorage) :
    IUserPasswordStore<ChronicleUser>,
    IUserEmailStore<ChronicleUser>,
    IUserSecurityStampStore<ChronicleUser>
{
    /// <inheritdoc/>
    public async Task<IdentityResult> CreateAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        await userStorage.Create(user);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeleteAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        await userStorage.Delete(user.Id);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<ChronicleUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await userStorage.GetById(userId);
    }

    /// <inheritdoc/>
    public async Task<ChronicleUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await userStorage.GetByUsername(normalizedUserName);
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedUserNameAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public Task<string> GetUserIdAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    /// <inheritdoc/>
    public Task<string?> GetUserNameAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username);
    }

    /// <inheritdoc/>
    public Task SetNormalizedUserNameAsync(ChronicleUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        // We store usernames as-is and normalize on query
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetUserNameAsync(ChronicleUser user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        await userStorage.Update(user);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public Task<string?> GetPasswordHashAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    /// <inheritdoc/>
    public Task<bool> HasPasswordAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    /// <inheritdoc/>
    public Task SetPasswordHashAsync(ChronicleUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ChronicleUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // Try to find by email first
        var user = await userStorage.GetByEmail(normalizedEmail);

        // If not found by email, try username (to support username-based login)
        user ??= await userStorage.GetByUsername(normalizedEmail);

        return user;
    }

    /// <inheritdoc/>
    public Task<string?> GetEmailAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    /// <inheritdoc/>
    public Task<bool> GetEmailConfirmedAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true); // Email confirmation not implemented yet
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedEmailAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email?.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public Task SetEmailAsync(ChronicleUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetEmailConfirmedAsync(ChronicleUser user, bool confirmed, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetNormalizedEmailAsync(ChronicleUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        // We normalize on query
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetSecurityStampAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp);
    }

    /// <inheritdoc/>
    public Task SetSecurityStampAsync(ChronicleUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
    }
}
