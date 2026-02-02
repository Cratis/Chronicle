// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage.Security;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IAuthenticationService"/>.
/// </summary>
/// <param name="userStorage">The user storage.</param>
/// <param name="users">The <see cref="IUsers"/> service.</param>
/// <param name="applications">The <see cref="IApplications"/> service.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="options">Chronicle options.</param>
/// <param name="logger">The logger.</param>
[Singleton]
internal sealed class AuthenticationService(
    IUserStorage userStorage,
    IUsers users,
#pragma warning disable CS9113 // Parameters are unread - this is due to conditional compilation with the DEVELOPMENT preprocessor symbol
    IApplications applications,
#pragma warning restore CS9113 // Parameters are unread - this is due to conditional compilation with the DEVELOPMENT preprocessor symbol
    IGrainFactory grainFactory,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    static readonly PasswordHasher<object> _passwordHasher = new();
    readonly Configuration.ChronicleOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<Storage.Security.User?> AuthenticateUser(Username username, Password password)
    {
        var user = await userStorage.GetByUsername(username);
        if (user?.IsActive is not true || user.PasswordHash is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success ? user : null;
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultAdminUser()
    {
        logger.CheckingForDefaultAdminUser();
        var existingUsers = await users.GetAll();
        if (existingUsers.Any(u => u.Username == _options.Authentication.DefaultAdminUsername))
        {
            logger.DefaultAdminUserAlreadyExist();
            return;
        }

        logger.CreatingDefaultAdminUser();

        var userId = Guid.NewGuid();
        var @event = new Grains.Security.InitialAdminUserAdded(
            _options.Authentication.DefaultAdminUsername,
            string.Empty);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(userId, @event);

        logger.DefaultAdminUserAdded();
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task EnsureDefaultClientCredentials()
    {
        const string defaultClientId = "chronicle-dev-client";
        const string defaultClientSecret = "chronicle-dev-secret";

        logger.CheckingForDefaultClientCredentials(defaultClientId);

        var existingApplications = await applications.GetAll();
        if (existingApplications.Any(a => a.ClientId == defaultClientId))
        {
            logger.DefaultClientCredentialsAlreadyExist(defaultClientId);
            return;
        }

        logger.CreatingDefaultClientCredentials(defaultClientId);

        await applications.Add(new AddApplication
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = defaultClientId,
            ClientSecret = defaultClientSecret
        });

        logger.DefaultClientCredentialsCreated(defaultClientId);
    }
#endif
}
