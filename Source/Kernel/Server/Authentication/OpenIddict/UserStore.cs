// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Represents a user store for Chronicle users that implements ASP.NET Identity interfaces.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserStore"/> class.
/// </remarks>
/// <param name="userStorage">The user storage.</param>
public class UserStore(IUserStorage userStorage) :
    IUserPasswordStore<User>,
    IUserEmailStore<User>,
    IUserSecurityStampStore<User>
{
    /// <inheritdoc/>
    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        await userStorage.Create(user);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        await userStorage.Delete(user.Id);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var userIdAsGuid))
        {
            return null;
        }

        return await userStorage.GetById(userIdAsGuid);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        // Identity normalizes usernames to uppercase, but we store them as-is
        // Try exact match first, then lowercase version since Identity normalizes to uppercase
        var user = await userStorage.GetByUsername(normalizedUserName);
        user ??= await userStorage.GetByUsername(normalizedUserName.ToLowerInvariant());

        return user;
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username.Value.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    /// <inheritdoc/>
    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username);
    }

    /// <inheritdoc/>
    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        // We store usernames as-is and normalize on query
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await userStorage.Update(user);
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash?.Value);
    }

    /// <inheritdoc/>
    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash?.Value));
    }

    /// <inheritdoc/>
    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // The normalizedEmail parameter contains the value entered by the user (not actually normalized in our case)
        // Try to find by email first (in case user entered an email)
        var user = await userStorage.GetByEmail(normalizedEmail);

        // If not found by email, try username (to support username-based login)
        // Try both the original value and lowercase version
        user ??= await userStorage.GetByUsername(normalizedEmail);
        user ??= await userStorage.GetByUsername(normalizedEmail.ToLowerInvariant());

        return user;
    }

    /// <inheritdoc/>
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email?.Value);
    }

    /// <inheritdoc/>
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true); // Email confirmation not implemented yet
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email?.Value.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        // We normalize on query
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp?.Value);
    }

    /// <inheritdoc/>
    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
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
