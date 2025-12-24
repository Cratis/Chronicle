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
        Console.WriteLine($"FindByNameAsync called with: {normalizedUserName}");

        // Identity normalizes usernames to uppercase, but we store them as-is
        // Try exact match first, then case-insensitive
        var user = await userStorage.GetByUsername(normalizedUserName);

        if (user is null && normalizedUserName != normalizedUserName.ToLowerInvariant())
        {
            // Try lowercase version
            user = await userStorage.GetByUsername(normalizedUserName.ToLowerInvariant());
        }

        Console.WriteLine($"FindByNameAsync result: {(user != null ? "Found" : "Not found")}");
        return user;
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
        Console.WriteLine($"GetPasswordHashAsync for user {user.Username}: Hash length = {user.PasswordHash?.Length ?? 0}");
        return Task.FromResult(user.PasswordHash);
    }

    /// <inheritdoc/>
    public Task<bool> HasPasswordAsync(ChronicleUser user, CancellationToken cancellationToken)
    {
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        Console.WriteLine($"HasPasswordAsync for user {user.Username}: {hasPassword}");
        return Task.FromResult(hasPassword);
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
        try
        {
            Console.WriteLine($"FindByEmailAsync called with: '{normalizedEmail}'");

            // The normalizedEmail parameter contains the value entered by the user (not actually normalized in our case)
            // Try to find by email first (in case user entered an email)
            var user = await userStorage.GetByEmail(normalizedEmail);
            Console.WriteLine($"Lookup by email: {(user != null ? "Found" : "Not found")}");

            // If not found by email, try username (to support username-based login)
            // Try both the original value and lowercase version
            if (user is null)
            {
                Console.WriteLine($"Trying username lookup with: '{normalizedEmail}'");
                user = await userStorage.GetByUsername(normalizedEmail);
                Console.WriteLine($"Lookup by username: {(user != null ? "Found" : "Not found")}");
            }

            if (user is null && normalizedEmail != normalizedEmail.ToLowerInvariant())
            {
                var lowerUsername = normalizedEmail.ToLowerInvariant();
                Console.WriteLine($"Trying lowercase username lookup with: '{lowerUsername}'");
                user = await userStorage.GetByUsername(lowerUsername);
                Console.WriteLine($"Lookup by lowercase username: {(user != null ? "Found" : "Not found")}");
            }

            if (user != null)
            {
                Console.WriteLine($"User found: ID={user.Id}, Username={user.Username}, HasPassword={!string.IsNullOrEmpty(user.PasswordHash)}");
            }
            else
            {
                Console.WriteLine("No user found with any lookup method");
            }

            return user;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error in FindByEmailAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
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
