// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Events.EventSequences;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Replays the authoritative roster with kernel proof and recovery requirements.
/// </summary>
/// <param name="proof">The kernel proof verifier.</param>
internal class RosterHistory(IKernelEventProof proof)
{
    /// <summary>
    /// Event types needed for roster replay, including redaction tombstones.
    /// </summary>
    internal static readonly EventType[] QueryTypes =
    [
        typeof(UserGrantedAdministrator).GetEventType(), typeof(UserRevokedAdministrator).GetEventType(),
        typeof(AdministratorPrivilegeMigrated).GetEventType(), typeof(UserRemoved).GetEventType(),
        new(GlobalEventTypes.Redaction, EventTypeGeneration.First)
    ];

    static readonly EventType _grantType = typeof(UserGrantedAdministrator).GetEventType();
    static readonly EventType _revokeType = typeof(UserRevokedAdministrator).GetEventType();
    static readonly EventType _markerType = typeof(AdministratorPrivilegeMigrated).GetEventType();

    /// <summary>
    /// Gets the users with verified active grants.
    /// </summary>
    internal HashSet<UserId> Granted { get; } = [];

    /// <summary>
    /// Gets the verified bootstrap grant recipients.
    /// </summary>
    internal HashSet<UserId> BootstrapGranted { get; } = [];

    /// <summary>
    /// Gets all verified grant recipients, including those later revoked.
    /// </summary>
    internal HashSet<UserId> EverGranted { get; } = [];

    /// <summary>
    /// Gets the users removed from the system.
    /// </summary>
    internal HashSet<UserId> Removed { get; } = [];

    /// <summary>
    /// Gets the recovery nonces observed in non-tombstone grant slots.
    /// </summary>
    internal HashSet<string> RecoveryNonces { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the latest trusted grant for each active recipient.
    /// </summary>
    internal Dictionary<UserId, UserGrantedAdministrator> LatestGrants { get; } = [];

    /// <summary>
    /// Gets the last roster slot, regardless of trust.
    /// </summary>
    internal EventSequenceNumber RawRosterTail { get; private set; } = EventSequenceNumber.BeforeFirst;

    /// <summary>
    /// Gets the last migration slot, regardless of trust.
    /// </summary>
    internal EventSequenceNumber RawMigrationTail { get; private set; } = EventSequenceNumber.BeforeFirst;

    /// <summary>
    /// Gets whether a verified migration marker has been read.
    /// </summary>
    internal bool MigrationCompleted { get; private set; }

    /// <summary>
    /// Gets whether operator recovery is required before applying ordinary grants.
    /// </summary>
    internal bool RecoveryRequired { get; private set; }

    /// <summary>
    /// Gets whether automatic grants are permanently disabled by this history.
    /// </summary>
    internal bool Compromised { get; private set; }

    /// <summary>
    /// Gets the first slot that failed verification.
    /// </summary>
    internal EventSequenceNumber? CompromisedAt { get; private set; }

    /// <summary>
    /// Signs roster family content, leaving other event types unchanged.
    /// </summary>
    /// <param name="content">The event content.</param>
    /// <param name="source">The event source.</param>
    /// <param name="predecessor">The preceding family slot.</param>
    /// <param name="proof">The kernel proof signer.</param>
    /// <returns>The signed content.</returns>
    internal static object WithProof(object content, EventSourceId source, EventSequenceNumber predecessor, IKernelEventProof proof) => content switch
    {
        UserGrantedAdministrator grant => grant with { Proof = proof.Sign(_grantType, source, predecessor, Body(grant)) },
        UserRevokedAdministrator revoke => revoke with { Proof = proof.Sign(_revokeType, source, predecessor, Body(revoke)) },
        AdministratorPrivilegeMigrated marker => marker with { Proof = proof.Sign(_markerType, source, predecessor, Body(marker)) },
        _ => content
    };

    /// <summary>
    /// Applies a stored event, returning a failure classification when recovery is required.
    /// </summary>
    /// <param name="event">The next stored event in sequence order.</param>
    /// <param name="deserialize">The content deserializer.</param>
    /// <returns>The verification failure classification, or null.</returns>
    internal string? Apply(AppendedEvent @event, Func<AppendedEvent, object> deserialize)
    {
        var context = @event.Context;
        if (context.EventType.Id == typeof(UserRemoved).GetEventType().Id)
        {
            if (Guid.TryParse(context.EventSourceId.Value, out var userId))
            {
                Removed.Add(userId);
                Granted.Remove(userId);
                LatestGrants.Remove(userId);
            }
            return null;
        }

        var roster = context.EventSourceId == Roster.EventSourceId;
        var migration = context.EventSourceId == Roster.MigrationEventSourceId;
        if (!roster && !migration) return null;
        var tombstone = context.EventType.Id == GlobalEventTypes.Redaction;
        if (!tombstone && !IsFamilyType(context.EventType.Id, roster)) return null;

        var content = Deserialize(@event, deserialize);
        if (tombstone && content is EventRedacted redacted && !IsFamilyType(redacted.OriginalEventType, roster)) return null;
        var predecessor = roster ? RawRosterTail : RawMigrationTail;
        if (roster) RawRosterTail = context.SequenceNumber;
        else RawMigrationTail = context.SequenceNumber;

        var nonce = !tombstone && roster && context.EventType.Id == _grantType.Id && content is UserGrantedAdministrator { Reason: GrantReason.OperatorRecovery } recovery
            ? RecoveryNonce(recovery.Evidence)
            : null;
        var freshNonce = nonce is not null && RecoveryNonces.Add(nonce);
        if (tombstone || @event.IsRevised) return Suspend(context.SequenceNumber, "tampered");
        if (!MatchesType(content, context.EventType.Id) || !Verify(content!, context, predecessor)) return Suspend(context.SequenceNumber, "unverifiable");

        switch (content)
        {
            case UserGrantedAdministrator { Reason: GrantReason.OperatorRecovery } grant:
                if (nonce is null) return Suspend(context.SequenceNumber, "unverifiable");
                if (!freshNonce) return null;
                RecoveryRequired = false;
                Grant(grant);
                break;
            case UserGrantedAdministrator grant:
                if (RecoveryRequired || (Compromised && grant.Reason is GrantReason.Bootstrap or GrantReason.UpgradeProvenance)) break;
                Grant(grant);
                break;
            case UserRevokedAdministrator revoke:
                Granted.Remove(revoke.UserId);
                LatestGrants.Remove(revoke.UserId);
                break;
            case AdministratorPrivilegeMigrated:
                MigrationCompleted = true;
                break;
        }
        return null;
    }

    static bool IsFamilyType(EventTypeId type, bool roster) => roster ? type == _grantType.Id || type == _revokeType.Id : type == _markerType.Id;

    static bool MatchesType(object? content, EventTypeId type) => content switch
    {
        UserGrantedAdministrator => type == _grantType.Id,
        UserRevokedAdministrator => type == _revokeType.Id,
        AdministratorPrivilegeMigrated => type == _markerType.Id,
        _ => false
    };

    static object? Deserialize(AppendedEvent @event, Func<AppendedEvent, object> deserialize)
    {
        try
        {
            return deserialize(@event);
        }
        catch (Exception)
        {
            // Unreadable stored content cannot establish kernel provenance.
            return null;
        }
    }

    static string Body(object content) => content switch
    {
        UserGrantedAdministrator grant => JsonSerializer.Serialize(new[] { grant.UserId.ToString(), ((int)grant.Reason).ToString(CultureInfo.InvariantCulture), grant.Evidence }),
        UserRevokedAdministrator revoke => JsonSerializer.Serialize(new[] { revoke.UserId.ToString(), revoke.RevokeReason }),
        AdministratorPrivilegeMigrated marker => JsonSerializer.Serialize(new[] { marker.Outcome, marker.UserId?.ToString() ?? string.Empty }),
        _ => throw new InvalidOperationException("The event is not a roster family event.")
    };

    static string? RecoveryNonce(string? evidence)
    {
        if (evidence is null) return null;
        try
        {
            using var document = JsonDocument.Parse(evidence);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("Nonce", out var nonce) && nonce.ValueKind == JsonValueKind.String
                    ? nonce.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    bool Verify(object content, EventContext context, EventSequenceNumber predecessor)
    {
        var signed = content switch
        {
            UserGrantedAdministrator grant => grant.Proof,
            UserRevokedAdministrator revoke => revoke.Proof,
            AdministratorPrivilegeMigrated marker => marker.Proof,
            _ => string.Empty
        };
        return proof.Verify(context.EventType, context.EventSourceId, predecessor, Body(content), signed);
    }

    string Suspend(EventSequenceNumber sequence, string classification)
    {
        RecoveryRequired = true;
        Compromised = true;
        CompromisedAt ??= sequence;
        Granted.Clear();
        BootstrapGranted.Clear();
        LatestGrants.Clear();
        return classification;
    }

    void Grant(UserGrantedAdministrator grant)
    {
        EverGranted.Add(grant.UserId);
        if (grant.Reason == GrantReason.Bootstrap) BootstrapGranted.Add(grant.UserId);
        if (Removed.Contains(grant.UserId)) return;
        Granted.Add(grant.UserId);
        LatestGrants[grant.UserId] = grant;
    }
}
