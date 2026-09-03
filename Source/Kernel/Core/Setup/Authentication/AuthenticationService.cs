// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
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
/// <param name="eventSerializer">The event serializer.</param>
/// <param name="logger">The logger.</param>
[Singleton]
internal sealed class AuthenticationService(
    IUserStorage userStorage,
    IApplicationStorage applicationStorage,
    IGrainFactory grainFactory,
    IOptions<Configuration.ChronicleOptions> options,
    IEventSerializer eventSerializer,
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
        if (!_options.Authentication.Enabled)
        {
            return;
        }

        logger.CheckingForDefaultAdminUser();

        var authentication = _options.Authentication;
        var adminUser = authentication.AdminUser;
        var effectiveUsername = authentication.EffectiveAdminUsername;
        var bootstrapPassword = adminUser?.Password ?? string.Empty;
#if DEVELOPMENT
        if (string.IsNullOrEmpty(bootstrapPassword))
        {
            bootstrapPassword = authentication.DefaultAdminPassword;
        }
#endif

        var eventSequence = grainFactory.GetEventLog();
        var existingAdmin = (await userStorage.GetAll()).FirstOrDefault(user =>
            string.Equals(user.Username, effectiveUsername, StringComparison.OrdinalIgnoreCase));
        if (existingAdmin is not null)
        {
            logger.DefaultAdminUserAlreadyExist();
            if (!existingAdmin.HasLoggedIn && !string.IsNullOrEmpty(bootstrapPassword))
            {
                await EnsureConfiguredPassword(
                    eventSequence,
                    existingAdmin.Id,
                    bootstrapPassword,
                    adminUser?.RequirePasswordChangeOnFirstLogin is true,
                    effectiveUsername);
            }
            return;
        }

        var userId = UserId.InitialAdministrator;
        var initialUser = new Security.InitialAdminUserAdded(effectiveUsername, adminUser?.Email ?? string.Empty);
        var initialResult = await AppendOnce(eventSequence, userId, initialUser);
        if (initialResult.HasConcurrencyViolations)
        {
            logger.DefaultAdminUserAlreadyExist();
            return;
        }
        if (!initialResult.IsSuccess)
        {
            throw new AdministratorBootstrapFailed("create the administrator");
        }

        if (!string.IsNullOrEmpty(bootstrapPassword))
        {
            logger.CreatingAdminUserWithConfiguredCredentials(effectiveUsername);
            await EnsureConfiguredPassword(
                eventSequence,
                userId,
                bootstrapPassword,
                adminUser?.RequirePasswordChangeOnFirstLogin is true,
                effectiveUsername);
            logger.AdminUserWithCredentialsCreated(effectiveUsername);
        }

        logger.DefaultAdminUserAdded();
    }

    /// <inheritdoc/>
    public async Task EnsureBootstrapClients()
    {
        if (!_options.Authentication.Enabled)
        {
            return;
        }

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
        if (!_options.Authentication.Enabled)
        {
            return;
        }

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

        // Write directly to storage for immediate availability. The event-driven path
        // (ApplicationsReactor) is asynchronous, which causes token endpoint failures
        // during integration test resets before the reactor has processed the event.
        await applicationStorage.Create(new Application
        {
            Id = Guid.NewGuid(),
            ClientId = (ClientId)defaultClientId,
            ClientSecret = (ClientSecret)hashedSecret,
            Type = (ApplicationType)"confidential",
            ConsentType = (ConsentType)"implicit",
            Permissions =
            [
                (Permission)"ept:token",
                (Permission)"gt:client_credentials",
                (Permission)"gt:password",
                (Permission)"gt:refresh_token"
            ]
        });

        logger.DefaultClientCredentialsCreated(defaultClientId);
    }
#endif

    async Task EnsureConfiguredPassword(
        IEventSequence eventSequence,
        UserId userId,
        string password,
        bool requirePasswordChange,
        string username)
    {
        var passwordHash = _passwordHasher.HashPassword(null!, password);
        var passwordResult = await AppendOnce(eventSequence, userId, new Security.UserPasswordChanged(passwordHash));
        if (!passwordResult.IsSuccess && !passwordResult.HasConcurrencyViolations)
        {
            throw new AdministratorBootstrapFailed("set the administrator password");
        }

        if (requirePasswordChange)
        {
            logger.RequiringPasswordChangeOnFirstLogin(username);
            var requirementResult = await AppendOnce(eventSequence, userId, new Security.PasswordChangeRequired());
            if (!requirementResult.IsSuccess && !requirementResult.HasConcurrencyViolations)
            {
                throw new AdministratorBootstrapFailed("require an administrator password change");
            }
        }
    }

    async Task<AppendResult> AppendOnce(IEventSequence eventSequence, UserId userId, object @event)
    {
        var eventType = @event.GetType().GetEventType();
        var concurrencyScope = new ConcurrencyScope(
            EventSequenceNumber.BeforeFirst,
            EventSourceId: true,
            EventStreamType: null,
            EventStreamId: null,
            EventSourceType: null,
            EventTypes: [eventType]);
        return await eventSequence.Append(
            EventSourceType.Default,
            userId,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            eventSerializer.Serialize(@event),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            concurrencyScope);
    }
}
