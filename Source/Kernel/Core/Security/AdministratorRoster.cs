// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Maintains the authoritative roster. Orleans' default non-reentrant scheduling serializes decisions;
/// guarded event batches also protect against duplicate activations.
/// </summary>
/// <param name="storage">The system storage.</param>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="serializer">The event serializer.</param>
/// <param name="proof">The kernel event proof.</param>
/// <param name="options">The server's authentication configuration.</param>
/// <param name="configuration">The server-local configuration providers.</param>
/// <param name="logger">The logger.</param>
public class AdministratorRoster(
    IStorage storage,
    IGrainFactory grainFactory,
    IEventSerializer serializer,
    IKernelEventProof proof,
    IOptions<ChronicleOptions> options,
    IConfiguration configuration,
    ILogger<AdministratorRoster> logger) : Grain, IAdministratorRoster
{
    static readonly EventType[] _rosterTypes = [typeof(UserGrantedAdministrator).GetEventType(), typeof(UserRevokedAdministrator).GetEventType()];
    static readonly EventType[] _rosterScopeTypes = [.. _rosterTypes, new(GlobalEventTypes.Redaction, EventTypeGeneration.First)];
    static readonly EventType[] _migrationScopeTypes = [typeof(AdministratorPrivilegeMigrated).GetEventType(), new(GlobalEventTypes.Redaction, EventTypeGeneration.First)];
    static readonly Action<ILogger, Exception?> _unprovenWarning = LoggerMessage.Define(LogLevel.Warning, new EventId(1, "AdministratorProvenanceUnproven"), "Administrator bootstrap provenance is unproven. No privilege was granted; server-local recovery is available.");
    static readonly Action<ILogger, string, Exception?> _recoveryWarning = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, "AdministratorRecovery"), "Administrator recovery: {Outcome}. Remove the server-local recovery configuration after use.");

    readonly HashSet<UserId> _grantTargets = [];
    RosterHistory _history = new(proof);

    IEventSequenceStorage Events => storage.GetEventStore(EventStoreName.System)
        .GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <inheritdoc/>
    public async Task Grant(UserId userId, GrantReason reason, string evidence)
    {
        await Mutate(async () =>
        {
            if (_history.Compromised && reason is GrantReason.Bootstrap or GrantReason.UpgradeProvenance)
            {
                logger.AutomaticAdministratorGrantsDisabled();
                return (true, []);
            }
            if ((await storage.System.Users.GetById(userId))?.IsActive is not true || _history.Removed.Contains(userId))
            {
                throw new InvalidOperationException("Administrator privilege requires an existing active user.");
            }
            var alreadyGranted = reason == GrantReason.Bootstrap ? _history.BootstrapGranted.Contains(userId) : _history.Granted.Contains(userId);
            return (true, alreadyGranted ? [] : [RosterEvent(new UserGrantedAdministrator(userId, reason, evidence))]);
        });
    }

    /// <inheritdoc/>
    public Task<bool> Revoke(UserId userId, string reason) => ChangeUser(userId, false, reason);

    /// <inheritdoc/>
    public Task<bool> RemoveUser(UserId userId) => ChangeUser(userId, true, "UserRemoved");

    /// <inheritdoc/>
    public async Task<bool> IsActiveAdministrator(UserId userId)
    {
        await Reload();
        return _history.Granted.Contains(userId) && (await storage.System.Users.GetById(userId))?.IsActive is true;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserId>> GetActiveAdministrators()
    {
        await Reload();
        var active = new List<UserId>();
        foreach (var userId in _history.Granted)
        {
            if ((await storage.System.Users.GetById(userId))?.IsActive is true) active.Add(userId);
        }
        return active;
    }

    /// <inheritdoc/>
    public async Task EnsureAdministratorPrivilege()
    {
        if (!options.Value.Authentication.Enabled) return;
        try
        {
            await InitializeAdministratorPrivilege();
        }
        catch (Exception exception)
        {
            logger.AdministratorInitializationFailed(exception.Message, exception);
            throw;
        }
    }

    async Task InitializeAdministratorPrivilege()
    {
        await Mutate(Migration, migration: true);
        var recovery = options.Value.Authentication.AdministratorRecovery;
        if (string.IsNullOrWhiteSpace(recovery.Username) && string.IsNullOrWhiteSpace(recovery.Nonce)) return;
        if (string.IsNullOrWhiteSpace(recovery.Username) || string.IsNullOrWhiteSpace(recovery.Nonce))
        {
            _recoveryWarning(logger, "refused; both Username and Nonce are required", null);
            return;
        }
        var applied = await Mutate(() => Recovery(recovery));
        _recoveryWarning(logger, applied ? "completed or nonce already consumed" : "refused; an active administrator exists or the target is not an active user", null);
    }

    Task<bool> ChangeUser(UserId userId, bool remove, string reason) => Mutate(async () =>
    {
        if (remove && _history.Removed.Contains(userId)) return (true, []);
        var granted = _history.Granted.Contains(userId);
        if (await WouldRemoveLastActiveAdministrator(userId))
        {
            return (false, []);
        }
        List<EventToAppend> events = [];
        if (granted) events.Add(RosterEvent(new UserRevokedAdministrator(userId, reason)));
        if (remove) events.Add(ToEvent(userId, new UserRemoved()));
        return (true, events.ToArray());
    });

    async Task<(bool Accepted, EventToAppend[] Events)> Migration()
    {
        if (_history.Compromised)
        {
            logger.AutomaticAdministratorGrantsDisabled();
            return (true, []);
        }
        if (_history.MigrationCompleted) return (true, []);
        var userId = await new AdministratorProvenance(storage, serializer).GetProvenAdministrator(options.Value.Authentication.EffectiveAdminUsername);
        List<EventToAppend> events = [];
        if (userId is not null && !_history.EverGranted.Contains(userId))
        {
            events.Add(RosterEvent(new UserGrantedAdministrator(userId, GrantReason.UpgradeProvenance, "Lowest-sequence InitialAdminUserAdded; configured username; active user; no later UserRemoved.")));
        }
        if (userId is null) _unprovenWarning(logger, null);
        events.Add(ToEvent(Roster.MigrationEventSourceId, new AdministratorPrivilegeMigrated(userId is null ? "Unproven" : "Proven", userId)));
        return (true, events.ToArray());
    }

    async Task<(bool Accepted, EventToAppend[] Events)> Recovery(AdministratorRecoveryOptions recovery)
    {
        if (_history.RecoveryNonces.Contains(recovery.Nonce)) return (true, []);
        if (!recovery.Force && await ActiveCount() != 0) return (false, []);
        var user = await storage.System.Users.GetByUsername(recovery.Username);
        if (user?.IsActive is not true || _history.Removed.Contains(user.Id)) return (false, []);
        var evidence = JsonSerializer.Serialize(new
        {
            recovery.Nonce,
            Host = Environment.MachineName,
            ConfigSource = RecoveryConfigurationSource(),
            recovery.Force
        });
        return (true, [RosterEvent(new UserGrantedAdministrator(user.Id, GrantReason.OperatorRecovery, evidence))]);
    }

    string RecoveryConfigurationSource()
    {
        const string key = "Authentication:AdministratorRecovery:Nonce";
        if (configuration is not IConfigurationRoot root) return key;
        foreach (var provider in root.Providers.Reverse())
        {
            foreach (var candidate in new[] { $"Cratis:Chronicle:{key}", $"Chronicle:{key}", key })
            {
                if (provider.TryGet(candidate, out _)) return $"{provider}: {candidate}";
            }
        }
        return key;
    }

    async Task<bool> WouldRemoveLastActiveAdministrator(UserId userId)
    {
        if (!_history.Granted.Contains(userId)) return false;
        if ((await storage.System.Users.GetById(userId))?.IsActive is not true) return false;
        return await ActiveCount() <= 1;
    }

    async Task<int> ActiveCount()
    {
        var count = 0;
        foreach (var userId in _history.Granted)
        {
            if ((await storage.System.Users.GetById(userId))?.IsActive is true) count++;
        }
        return count;
    }

    async Task<bool> Mutate(Func<Task<(bool Accepted, EventToAppend[] Events)>> decide, bool migration = false)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await Reload();
            var decision = await decide();
            if (!decision.Accepted || decision.Events.Length == 0) return decision.Accepted;
            if (decision.Events.Count(@event => @event.EventSourceId == Roster.EventSourceId) > 1 ||
                decision.Events.Count(@event => @event.EventSourceId == Roster.MigrationEventSourceId) > 1)
            {
                throw new InvalidOperationException("A roster decision cannot contain more than one event per family.");
            }
            var result = await grainFactory.GetEventLog().AppendMany(
                decision.Events, CorrelationId.New(), [], Identity.System, Scopes(decision.Events, migration));
            if (result.HasConcurrencyViolations) continue;
            if (!result.IsSuccess || !result.ConcurrencyCheckPerformed)
            {
                logger.AdministratorAppendFailed(result.ConcurrencyCheckPerformed, string.Join("; ", result.Errors));
                throw new InvalidOperationException("The guarded administrator roster change could not be appended.");
            }
            return true;
        }
        throw new InvalidOperationException("The administrator roster changed repeatedly; retry the operation.");
    }

    ConcurrencyScopes Scopes(EventToAppend[] events, bool migration)
    {
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [Roster.EventSourceId] = new(_history.RawRosterTail, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: _rosterScopeTypes)
        };
        if (migration)
        {
            scopes[Roster.MigrationEventSourceId] = new(_history.RawMigrationTail, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: _migrationScopeTypes);
        }
        foreach (var userId in _grantTargets)
        {
            scopes[userId] = new(EventSequenceNumber.BeforeFirst, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: [typeof(UserRemoved).GetEventType()]);
        }
        foreach (var @event in events.Where(@event => @event.EventType == typeof(UserRemoved).GetEventType()))
        {
            scopes[@event.EventSourceId] = new(EventSequenceNumber.BeforeFirst, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: [typeof(UserRemoved).GetEventType()]);
        }
        return new(scopes);
    }

    async Task Reload()
    {
        var history = new RosterHistory(proof);
        var tail = await Events.GetTailSequenceNumber(RosterHistory.QueryTypes);
        _grantTargets.Clear();
        if (tail.IsActualValue)
        {
            using var cursor = await Events.GetRange(EventSequenceNumber.First, tail, eventTypes: RosterHistory.QueryTypes);
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    var failure = history.Apply(@event, serializer.Deserialize);
                    if (failure is not null) logger.AdministratorRosterVerificationFailed(@event.Context.SequenceNumber, failure);
                }
            }
        }
        _history = history;
    }

    EventToAppend RosterEvent(object content)
    {
        if (content is UserGrantedAdministrator grant) _grantTargets.Add(grant.UserId);
        return ToEvent(Roster.EventSourceId, content);
    }

    EventToAppend ToEvent(EventSourceId source, object content)
    {
        var predecessor = source == Roster.EventSourceId ? _history.RawRosterTail : _history.RawMigrationTail;
        content = RosterHistory.WithProof(content, source, predecessor, proof);
        return new(EventSourceType.Default, source, EventStreamType.All, EventStreamId.Default, content.GetType().GetEventType(), [], serializer.Serialize(content));
    }
}
