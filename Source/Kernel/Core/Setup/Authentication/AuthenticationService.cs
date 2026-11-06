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
using ConfiguredGrant = Cratis.Chronicle.Security.ConfiguredGrant;
using IStoreEntitlements = Cratis.Chronicle.Security.IStoreEntitlements;
using PrincipalKey = Cratis.Chronicle.Security.PrincipalKey;
#if DEVELOPMENT
using StoreAccess = Cratis.Chronicle.Security.StoreAccess;
#endif

namespace Cratis.Chronicle.Setup.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IAuthenticationService"/>.
/// </summary>
/// <param name="userStorage">The user storage.</param>
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> service.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="options">Chronicle options.</param>
/// <param name="eventSerializer">The event serializer.</param>
/// <param name="setupTokens">The initial administrator setup tokens.</param>
/// <param name="logger">The logger.</param>
[Singleton]
internal sealed class AuthenticationService(
    IUserStorage userStorage,
    IApplicationStorage applicationStorage,
    IGrainFactory grainFactory,
    IOptions<Configuration.ChronicleOptions> options,
    IEventSerializer eventSerializer,
    Security.IInitialAdminSetupTokens setupTokens,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    static readonly PasswordHasher<object> _passwordHasher = new();
    readonly Configuration.ChronicleOptions _options = options.Value;
    readonly InitialAdminSetupTokenLogging _setupTokenLogging = new(setupTokens, options, logger);

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
        if (string.IsNullOrWhiteSpace(effectiveUsername))
        {
            throw new AdministratorBootstrapFailed("resolve a nonblank administrator username");
        }

        var bootstrapPassword = adminUser?.Password ?? string.Empty;
#if DEVELOPMENT
        if (string.IsNullOrEmpty(bootstrapPassword))
        {
            bootstrapPassword = authentication.DefaultAdminPassword;
        }
#endif

        WarnIfConfiguredPasswordIsShort(bootstrapPassword);

        var eventSequence = grainFactory.GetEventLog();
        var existingAdmin = (await userStorage.GetAll()).FirstOrDefault(user =>
            string.Equals(user.Username, effectiveUsername, StringComparison.OrdinalIgnoreCase));
        if (existingAdmin is not null)
        {
            logger.DefaultAdminUserAlreadyExist();
            if (!existingAdmin.HasLoggedIn && string.IsNullOrEmpty(existingAdmin.PasswordHash?.Value) && !string.IsNullOrEmpty(bootstrapPassword))
            {
                await EnsureConfiguredPassword(
                    eventSequence,
                    existingAdmin.Id,
                    bootstrapPassword,
                    adminUser?.RequirePasswordChangeOnFirstLogin is true,
                    effectiveUsername);
            }
            _setupTokenLogging.LogIfRequired(existingAdmin, bootstrapPassword);
            return;
        }

        var userId = UserId.InitialAdministrator;
        var initialUser = new Security.InitialAdminUserAdded(effectiveUsername, adminUser?.Email ?? string.Empty);
        var initialResult = await AppendOnce(eventSequence, userId, initialUser);

        // A competing silo may have appended creation and crashed before setting credentials.
        // Continue the independently guarded password step instead of abandoning bootstrap.
        if (!initialResult.IsSuccess && !initialResult.HasConcurrencyViolations)
        {
            throw new AdministratorBootstrapFailed("create the administrator");
        }

        await Security.UserProjection.WaitForAdministrator(userStorage, userId, effectiveUsername);

        await GrantBootstrapPrivilege(initialResult, userId);

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

        // Re-read after creation so a concurrently configured password suppresses token issuance.
        _setupTokenLogging.LogIfRequired(await userStorage.GetById(userId), bootstrapPassword);
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
        logger.BootstrappingClients(clients.Count());

        var existingApplications = (await applicationStorage.GetAll()).ToArray();
        var resolved = existingApplications.ToDictionary(application => application.ClientId.ToString(), application => application.Id.ToString());

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
            resolved[client.ClientId] = applicationId;

            logger.BootstrapClientRegistered(client.ClientId);
        }
        await ReconcileConfiguredGrants(resolved);
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
        var applicationId = Guid.NewGuid();
        await applicationStorage.Create(new Application
        {
            Id = applicationId,
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
        var resolved = existingApplications.ToDictionary(application => application.ClientId.ToString(), application => application.Id.ToString());
        resolved[defaultClientId] = applicationId.ToString();
        await ReconcileConfiguredGrants(resolved);
    }
#endif

    static ConfiguredGrant ToGrant(PrincipalKey holder, Configuration.ConfiguredEventStoreAccess entry) =>
        new(holder, entry.EventStore is null ? null : new Concepts.EventStoreName(entry.EventStore), entry.Namespace is null ? null : new Concepts.EventStoreNamespaceName(entry.Namespace), entry.Access);

    async Task ReconcileConfiguredGrants(Dictionary<string, string> applications)
    {
        var grants = _options.Authentication.EventStoreAccess
            .Select(entry => ToGrant(new(entry.Issuer, entry.Subject, entry.IsClient), entry))
            .ToList();
        foreach (var client in _options.Clients)
        {
            if (!applications.TryGetValue(client.ClientId, out var subject)) continue;
            grants.AddRange(client.EventStores.Select(entry => ToGrant(new(PrincipalKey.LocalIssuer, subject), entry)));
        }
#if DEVELOPMENT
        // The development client has a normal, visible configuration grant, not an authorization bypass.
        if (applications.TryGetValue("chronicle-dev-client", out var developmentSubject))
        {
            grants.Add(new(new(PrincipalKey.LocalIssuer, developmentSubject), null, null, StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage));
        }
#endif
        await grainFactory.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Reconcile(grants);
    }

    Task GrantBootstrapPrivilege(AppendResult initialResult, UserId userId) => initialResult.IsSuccess
        ? grainFactory.GetGrain<Security.IAdministratorRoster>(AdministratorRoster.Key)
            .Grant(userId, GrantReason.Bootstrap, "Successfully appended InitialAdminUserAdded for the deterministic initial administrator.")
        : Task.CompletedTask;

    void WarnIfConfiguredPasswordIsShort(string password)
    {
        if (password.Length is > 0 and < Security.PasswordPolicy.MinimumLength)
        {
            logger.ConfiguredAdminPasswordIsShort(Security.PasswordPolicy.MinimumLength);
        }
    }

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

        if (passwordResult.IsSuccess)
        {
            await Security.UserProjection.WaitForPassword(userStorage, userId, passwordHash);
        }
        else
        {
            await Security.UserProjection.WaitForInitializedPassword(userStorage, userId);
        }

        if (requirePasswordChange)
        {
            logger.RequiringPasswordChangeOnFirstLogin(username);
            var requirementResult = await AppendOnce(eventSequence, userId, new Security.PasswordChangeRequired());
            if (!requirementResult.IsSuccess && !requirementResult.HasConcurrencyViolations)
            {
                throw new AdministratorBootstrapFailed("require an administrator password change");
            }
            if (requirementResult.IsSuccess)
            {
                await Security.UserProjection.WaitForPasswordChangeRequirement(userStorage, userId);
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
