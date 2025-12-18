// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using StorageChronicleClient = Cratis.Chronicle.Storage.Security.ChronicleClient;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IAuthenticationService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthenticationService"/> class.
/// </remarks>
/// <param name="userStorage">The user storage.</param>
/// <param name="clientCredentialsStorage">The client credentials storage.</param>
/// <param name="applicationStorage">The application storage.</param>
/// <param name="options">Chronicle options.</param>
public class AuthenticationService(
    IUserStorage userStorage,
    IClientCredentialsStorage clientCredentialsStorage,
    IApplicationStorage applicationStorage,
    IOptions<Configuration.ChronicleOptions> options) : IAuthenticationService
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

        var isValid = VerifyPassword(password, user.PasswordHash);
        return isValid ? user : null;
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultAdminUser()
    {
        var adminUser = await userStorage.GetByUsername(_options.Authentication.DefaultAdminUsername);
        if (adminUser is not null)
        {
            return;
        }

        var password = _options.Authentication.DefaultAdminPassword ?? "admin";
        var passwordHash = HashPassword(password);
        var now = DateTimeOffset.UtcNow;

        var user = new ChronicleUser(
            Id: Guid.NewGuid().ToString(),
            Username: _options.Authentication.DefaultAdminUsername,
            Email: null,
            PasswordHash: passwordHash,
            SecurityStamp: Guid.NewGuid().ToString(),
            IsActive: true,
            CreatedAt: now,
            LastModifiedAt: null);

        await userStorage.Create(user);
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task EnsureDefaultClientCredentials()
    {
        const string defaultClientId = "chronicle-dev-client";
        const string defaultClientSecret = "chronicle-dev-secret";

        var existingClient = await clientCredentialsStorage.GetByClientId(defaultClientId);
        if (existingClient is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var clientSecretHash = HashPassword(defaultClientSecret);

        // Create ChronicleClient for client_credentials flow
        var client = new StorageChronicleClient(
            Id: Guid.NewGuid().ToString(),
            ClientId: defaultClientId,
            ClientSecret: clientSecretHash,
            IsActive: true,
            CreatedAt: now,
            LastModifiedAt: null);

        await clientCredentialsStorage.Create(client);

        // Create corresponding Application entity for OpenIddict
        var application = new Application(
            Id: Guid.NewGuid().ToString(),
            ClientId: defaultClientId,
            ClientSecret: clientSecretHash,
            DisplayName: "Chronicle Development Client",
            Type: OpenIddictConstants.ClientTypes.Confidential,
            ConsentType: OpenIddictConstants.ConsentTypes.Implicit,
            Permissions: [
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken
            ],
            Requirements: [],
            RedirectUris: [],
            PostLogoutRedirectUris: [],
            Properties: System.Collections.Immutable.ImmutableDictionary<string, System.Text.Json.JsonElement>.Empty);

        await applicationStorage.Create(application);
    }
#endif

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

    static bool VerifyPassword(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var testHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return hash.SequenceEqual(testHash);
    }
}
