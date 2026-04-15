// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
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
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> service.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="options">Chronicle options.</param>
/// <param name="logger">The logger.</param>
[Singleton]
internal sealed class AuthenticationService(
    IUserStorage userStorage,
    IApplicationStorage applicationStorage,
    IGrainFactory grainFactory,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    static readonly PasswordHasher<object> _passwordHasher = new();
    readonly Configuration.ChronicleOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<User?> AuthenticateUser(Username username, Password password)
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
        var existingUsers = await userStorage.GetAll();
        if (existingUsers.Any(u => u.Username == _options.Authentication.DefaultAdminUsername))
        {
            logger.DefaultAdminUserAlreadyExist();
            return;
        }

        logger.CreatingDefaultAdminUser();

        var userId = Guid.NewGuid();
        var @event = new Security.InitialAdminUserAdded(
            _options.Authentication.DefaultAdminUsername,
            string.Empty);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(userId, @event);

#if DEVELOPMENT
        if (!string.IsNullOrEmpty(_options.Authentication.DefaultAdminPassword))
        {
            logger.SettingDefaultAdminPassword();

            // null! is safe here: ASP.NET Identity's default PasswordHasher ignores the user parameter
            var passwordHash = _passwordHasher.HashPassword(null!, _options.Authentication.DefaultAdminPassword);
            await eventSequence.Append(userId, new Security.UserPasswordChanged(passwordHash));
            logger.DefaultAdminPasswordSet();
        }
#endif

        logger.DefaultAdminUserAdded();
    }

    /// <inheritdoc/>
    public async Task EnsureBootstrapClients()
    {
        var clients = _options.Clients;
        if (!clients.Any())
        {
            return;
        }

        logger.BootstrappingClients(clients.Count());

        var existingApplications = await applicationStorage.GetAll();

        foreach (var client in clients)
        {
            if (string.IsNullOrEmpty(client.ClientId) || string.IsNullOrEmpty(client.ClientSecret))
            {
                logger.SkippingInvalidBootstrapClient(client.ClientId ?? "(empty)");
                continue;
            }

            if (existingApplications.Any(a => a.ClientId == client.ClientId))
            {
                logger.BootstrapClientAlreadyExists(client.ClientId);
                continue;
            }

            logger.RegisteringBootstrapClient(client.ClientId);

            var hashedSecret = _passwordHasher.HashPassword(null!, client.ClientSecret);
            var applicationId = Guid.NewGuid().ToString();
            var @event = new Security.ApplicationAdded(
                client.ClientId,
                hashedSecret);

            var eventSequence = grainFactory.GetEventLog();
            await eventSequence.Append(applicationId, @event);

            logger.BootstrapClientRegistered(client.ClientId);
        }
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task EnsureDefaultClientCredentials()
    {
        const string defaultClientId = "chronicle-dev-client";
        const string defaultClientSecret = "chronicle-dev-secret";

        logger.CheckingForDefaultClientCredentials(defaultClientId);

        var existingApplications = await applicationStorage.GetAll();
        if (existingApplications.Any(a => a.ClientId == defaultClientId))
        {
            logger.DefaultClientCredentialsAlreadyExist(defaultClientId);
            return;
        }

        logger.CreatingDefaultClientCredentials(defaultClientId);

        // Hash the secret to match how other application secrets are stored
        var hashedSecret = _passwordHasher.HashPassword(null!, defaultClientSecret);
        var applicationId = Guid.NewGuid().ToString();
        var @event = new Security.ApplicationAdded(
            defaultClientId,
            hashedSecret);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(applicationId, @event);

        logger.DefaultClientCredentialsCreated(defaultClientId);
    }
#endif
}
