// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security;

/// <summary>Replays explicit grants from their dedicated System event source.</summary>
/// <param name="proof">The kernel proof verifier.</param>
internal sealed class StoreEntitlementsHistory(IKernelEventProof proof)
{
    /// <summary>The dedicated source for grant changes.</summary>
    internal static readonly EventSourceId EventSourceId = new("store-entitlements");

    /// <summary>The event types participating in grant concurrency.</summary>
    internal static readonly EventType[] ScopeTypes =
    [
        typeof(EventStoreAccessGranted).GetEventType(), typeof(EventStoreAccessRevoked).GetEventType(),
        new(GlobalEventTypes.Redaction, EventTypeGeneration.First)
    ];

    /// <summary>The event types needed to rebuild grants.</summary>
    internal static readonly EventType[] QueryTypes = [.. ScopeTypes, typeof(ApplicationRemoved).GetEventType(), typeof(UserRemoved).GetEventType()];
    readonly List<EventStoreAccessGrant> _grants = [];
    readonly HashSet<string> _removed = new(StringComparer.Ordinal);

    /// <summary>Gets the last committed grant slot, including unproven slots.</summary>
    internal EventSequenceNumber Tail { get; private set; } = EventSequenceNumber.BeforeFirst;

    /// <summary>Gets the replayed change version.</summary>
    internal long Version { get; private set; }

    /// <summary>Gets the current proven grants.</summary>
    internal IReadOnlyList<EventStoreAccessGrant> Grants => _grants;

    /// <summary>Compares the identity of two grants.</summary>
    /// <param name="left">The first grant.</param>
    /// <param name="right">The second grant.</param>
    /// <returns>Whether both grants have the same identity.</returns>
    internal static bool SameIdentity(EventStoreAccessGrant left, EventStoreAccessGrant right) =>
        left.Holder == right.Holder && StringComparer.OrdinalIgnoreCase.Equals(left.EventStore?.Value, right.EventStore?.Value) && left.Namespace == right.Namespace;

    /// <summary>Binds a change to its source and committed predecessor.</summary>
    /// <param name="content">The unsigned change.</param>
    /// <param name="predecessor">The committed predecessor.</param>
    /// <param name="proof">The proof signer.</param>
    /// <returns>The signed change.</returns>
    /// <exception cref="InvalidOperationException">The content is not an entitlement change.</exception>
    internal static object WithProof(object content, EventSequenceNumber predecessor, IKernelEventProof proof) => content switch
    {
        EventStoreAccessGranted grant => grant with { Proof = proof.Sign(typeof(EventStoreAccessGranted).GetEventType(), EventSourceId, predecessor, Body(grant)) },
        EventStoreAccessRevoked revoke => revoke with { Proof = proof.Sign(typeof(EventStoreAccessRevoked).GetEventType(), EventSourceId, predecessor, Body(revoke)) },
        _ => throw new InvalidOperationException("Unsupported event store entitlement event.")
    };

    /// <summary>Checks whether a local principal was removed.</summary>
    /// <param name="holder">The principal.</param>
    /// <returns>Whether it was removed.</returns>
    internal bool IsRemoved(PrincipalKey holder) => holder.Issuer == PrincipalKey.LocalIssuer && _removed.Contains(holder.Subject);

    /// <summary>Applies an ordered event slot after verifying its provenance.</summary>
    /// <param name="appended">The committed slot.</param>
    /// <param name="content">The deserialized content, or null when unreadable.</param>
    internal void Apply(AppendedEvent appended, object? content)
    {
        var context = appended.Context;
        if (IsRemoval(context))
        {
            _removed.Add(context.EventSourceId.Value);
            _grants.RemoveAll(grant => IsRemoved(grant.Holder));
            Version++;
            return;
        }
        if (context.EventSourceId != EventSourceId) return;
        var predecessor = Tail;
        Tail = context.SequenceNumber;
        Version++;
        if (IsUnproven(appended, content, predecessor))
        {
            _grants.Clear();
            return;
        }
        ApplyProven(content);
    }

    static bool IsRemoval(EventContext context) => context.EventType == typeof(ApplicationRemoved).GetEventType() || context.EventType == typeof(UserRemoved).GetEventType();

    static bool IsOrdinaryGrant(EventStoreAccessGranted grant) => grant.EventStore is null || !SystemStoreAccess.IsSystem(grant.EventStore);

    static string Body(EventStoreAccessGranted grant) => JsonSerializer.Serialize(grant with { Proof = string.Empty });
    static string Body(EventStoreAccessRevoked revoke) => JsonSerializer.Serialize(revoke with { Proof = string.Empty });

    void ApplyProven(object? content)
    {
        if (content is EventStoreAccessGranted grant)
        {
            var entry = new EventStoreAccessGrant(grant.Holder, grant.EventStore, grant.Namespace, grant.Access, grant.Source);
            _grants.RemoveAll(existing => SameIdentity(existing, entry));
            if (!IsRemoved(grant.Holder) && IsOrdinaryGrant(grant)) _grants.Add(entry);
        }
        else if (content is EventStoreAccessRevoked revoke)
        {
            var entry = new EventStoreAccessGrant(revoke.Holder, revoke.EventStore, revoke.Namespace, StoreAccess.None, EventStoreAccessGrantSource.Command);
            _grants.RemoveAll(existing => SameIdentity(existing, entry));
        }
    }

    bool IsUnproven(AppendedEvent appended, object? content, EventSequenceNumber predecessor) => appended.Context.EventType.Id == GlobalEventTypes.Redaction
        || appended.IsRevised || !Verified(appended.Context, content, predecessor);

    bool Verified(EventContext context, object? content, EventSequenceNumber predecessor) => content switch
    {
        EventStoreAccessGranted grant => context.EventType == typeof(EventStoreAccessGranted).GetEventType() && proof.Verify(context.EventType, EventSourceId, predecessor, Body(grant), grant.Proof),
        EventStoreAccessRevoked revoke => context.EventType == typeof(EventStoreAccessRevoked).GetEventType() && proof.Verify(context.EventType, EventSourceId, predecessor, Body(revoke), revoke.Proof),
        _ => false
    };
}
