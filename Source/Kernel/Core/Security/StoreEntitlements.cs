// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Security;

/// <summary>Owns the authoritative, explicitly granted event store access.</summary>
/// <param name="storage">The System event log storage.</param>
/// <param name="grains">The event log grain factory.</param>
/// <param name="serializer">The stored event serializer.</param>
/// <param name="proof">The kernel proof service.</param>
/// <param name="caches">The per-silo snapshot broadcaster.</param>
public class StoreEntitlements(IStorage storage, IGrainFactory grains, IEventSerializer serializer, IKernelEventProof proof, IStoreEntitlementsCacheClient caches) : Grain, IStoreEntitlements
{
    StoreEntitlementsHistory _history = new(proof);
    IEventSequenceStorage Events => storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <inheritdoc/>
    public async Task Grant(EventStoreAccessGrant grant)
    {
        Validate(grant);
        await Mutate(() =>
        {
            if (_history.IsRemoved(grant.Holder)) throw new ArgumentException("The principal has been removed.", nameof(grant));
            var existing = _history.Grants.FirstOrDefault(candidate => StoreEntitlementsHistory.SameIdentity(candidate, grant));
            return existing == grant ? [] : [new EventStoreAccessGranted(grant.Holder, grant.EventStore, grant.Namespace, grant.Access, grant.Source)];
        });
    }

    /// <inheritdoc/>
    public Task<bool> Revoke(EventStoreAccessGrant grant) => Mutate(() => _history.Grants.Any(candidate => StoreEntitlementsHistory.SameIdentity(candidate, grant))
        ? [new EventStoreAccessRevoked(grant.Holder, grant.EventStore, grant.Namespace)] : []);

    /// <inheritdoc/>
    public async Task Reconcile(IEnumerable<ConfiguredGrant> grants)
    {
        var desired = grants.Select(grant => new EventStoreAccessGrant(grant.Holder, grant.EventStore, grant.Namespace, grant.Access, EventStoreAccessGrantSource.Configuration)).ToArray();
        foreach (var grant in desired) Validate(grant);
        if (desired.Where((grant, index) => desired.Take(index).Any(other => StoreEntitlementsHistory.SameIdentity(grant, other))).Any()) throw new ArgumentException("Configured grants must have distinct identities.", nameof(grants));
        await Mutate(() => ReconcileEvents(desired));
    }

    /// <inheritdoc/>
    public async Task<StoreEntitlementsState> GetSnapshot()
    {
        await Reload();
        return State();
    }

    /// <inheritdoc/>
    public async Task<long> GetVersion()
    {
        await Reload();
        return _history.Version;
    }

    static void Validate(EventStoreAccessGrant grant)
    {
        if (!IsIdentified(grant.Holder)) throw new ArgumentException("A grant requires an identified principal.", nameof(grant));
        if (!IsOrdinaryStore(grant.EventStore)) throw new ArgumentException("A grant requires a non-System event store or all event stores.", nameof(grant));
        if (grant.Access == StoreAccess.None || (grant.Access & ~(StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage)) != StoreAccess.None) throw new ArgumentException("A grant requires supported access flags.", nameof(grant));
        if (!Enum.IsDefined(grant.Source)) throw new ArgumentException("A grant requires a supported source.", nameof(grant));
    }

    static bool IsIdentified(PrincipalKey holder) => !string.IsNullOrWhiteSpace(holder.Issuer) && !string.IsNullOrWhiteSpace(holder.Subject) && !holder.IsUnidentified;

    static bool IsOrdinaryStore(EventStoreName? store) => store is null || (!SystemStoreAccess.IsSystem(store) && !string.IsNullOrWhiteSpace(store.Value));

    object[] ReconcileEvents(EventStoreAccessGrant[] desired)
    {
        var changes = new List<object>();
        changes.AddRange(_history.Grants
            .Where(existing => existing.Source == EventStoreAccessGrantSource.Configuration && !desired.Any(grant => StoreEntitlementsHistory.SameIdentity(existing, grant)))
            .Select(existing => new EventStoreAccessRevoked(existing.Holder, existing.EventStore, existing.Namespace)));
        foreach (var grant in desired.Where(grant => !_history.IsRemoved(grant.Holder)))
        {
            var existing = _history.Grants.FirstOrDefault(candidate => StoreEntitlementsHistory.SameIdentity(candidate, grant));
            if (existing?.Source == EventStoreAccessGrantSource.Command || existing == grant) continue;
            changes.Add(new EventStoreAccessGranted(grant.Holder, grant.EventStore, grant.Namespace, grant.Access, grant.Source));
        }
        return changes.ToArray();
    }

    async Task<bool> Mutate(Func<object[]> decide)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await Reload();
            var changes = decide();
            if (changes.Length == 0) return false;

            // Proofs bind to committed predecessors, so each append contains one entitlement event.
            var content = StoreEntitlementsHistory.WithProof(changes[0], _history.Tail, proof);
            var type = content.GetType().GetEventType();
            var @event = new EventToAppend(EventSourceType.Default, StoreEntitlementsHistory.EventSourceId, EventStreamType.All, EventStreamId.Default, type, [], serializer.Serialize(content));
            var result = await grains.GetEventLog().AppendMany([@event], CorrelationId.New(), [], Identity.System, Scopes(content));
            if (result.HasConcurrencyViolations) continue;
            if (!result.IsSuccess || !result.ConcurrencyCheckPerformed) throw new InvalidOperationException("The guarded event store entitlement change could not be appended.");
            await Reload();
            await caches.Refresh(State());
            if (changes.Length == 1) return true;
            attempt = -1;
        }
        throw new InvalidOperationException("The event store entitlements changed repeatedly; retry the operation.");
    }

    ConcurrencyScopes Scopes(object content)
    {
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [StoreEntitlementsHistory.EventSourceId] = new(_history.Tail, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: StoreEntitlementsHistory.ScopeTypes)
        };
        if (content is EventStoreAccessGranted grant && grant.Holder.Issuer == PrincipalKey.LocalIssuer)
        {
            scopes[grant.Holder.Subject] = new(EventSequenceNumber.BeforeFirst, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: [typeof(ApplicationRemoved).GetEventType(), typeof(UserRemoved).GetEventType()]);
        }
        return new(scopes);
    }

    StoreEntitlementsState State() => new(_history.Version, _history.Grants.ToArray());

    async Task Reload()
    {
        var history = new StoreEntitlementsHistory(proof);
        var tail = await Events.GetTailSequenceNumber(StoreEntitlementsHistory.QueryTypes);
        if (tail.IsActualValue)
        {
            using var cursor = await Events.GetRange(EventSequenceNumber.First, tail, eventTypes: StoreEntitlementsHistory.QueryTypes);
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    object? content;
                    try { content = serializer.Deserialize(@event); }
                    catch { content = null; }
                    history.Apply(@event, content);
                }
            }
        }
        _history = history;
    }
}
