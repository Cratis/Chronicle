// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IAuthenticationService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthenticationService"/> class.
/// </remarks>
/// <param name="userStorage">The user storage.</param>
/// <param name="applicationManager">The OpenIddict application manager.</param>
/// <param name="options">Chronicle options.</param>
/// <param name="logger">The logger.</param>
public class AuthenticationService(
    IUserStorage userStorage,
    IOpenIddictApplicationManager applicationManager,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    readonly Configuration.ChronicleOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<ChronicleUser?> AuthenticateUser(string username, string password)
    {
        var user = await userStorage.GetByUsername(username);
        if (user?.IsActive is not true || user.PasswordHash is null)
        {
            return null;
        }

        var isValid = HashHelper.Verify(password, user.PasswordHash);
        return isValid ? user : null;
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultAdminUser()
    {
        Console.WriteLine($"Checking for default admin user: {_options.Authentication.DefaultAdminUsername}");

        var adminUser = await userStorage.GetByUsername(_options.Authentication.DefaultAdminUsername);
        if (adminUser is not null)
        {
            Console.WriteLine("Default admin user already exists");
            return;
        }

        Console.WriteLine("Creating default admin user...");
        var password = _options.Authentication.DefaultAdminPassword ?? "admin";
        var passwordHash = HashHelper.Hash(password);
        var now = DateTimeOffset.UtcNow;

        var user = new ChronicleUser
        {
            Id = Guid.NewGuid().ToString(),
            Username = _options.Authentication.DefaultAdminUsername,
            Email = null,
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedAt = now,
            LastModifiedAt = null
        };

        await userStorage.Create(user);
        Console.WriteLine($"Default admin user created with username: {user.Username}");
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task EnsureDefaultClientCredentials()
    {
        const string defaultClientId = "chronicle-dev-client";
        const string defaultClientSecret = "chronicle-dev-secret";

        logger.CheckingForDefaultClientCredentials(defaultClientId);

        // Check if already exists in OpenIddict
        var existingClient = await applicationManager.FindByClientIdAsync(defaultClientId);
        if (existingClient is not null)
        {
            logger.DefaultClientCredentialsAlreadyExist(defaultClientId);
            return;
        }

        logger.CreatingDefaultClientCredentials(defaultClientId);

        // Create the application using OpenIddict's manager
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = defaultClientId,
            ClientSecret = defaultClientSecret,  // OpenIddict will hash this
            DisplayName = "Chronicle Development Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken
            }
        };

        await applicationManager.CreateAsync(descriptor);
        logger.DefaultClientCredentialsCreated(defaultClientId);
    }
#endif
}
