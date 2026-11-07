// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>The shared audit and enforcement rule for explicit store access.</summary>
public static class StoreEntitlementEnforcement
{
    static readonly ConditionalWeakTable<ILoggerFactory, AuditTracker> _trackers = new();
    static readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    /// <summary>Applies audit mode only to an ordinary, successfully evaluated refusal.</summary>
    /// <param name="decision">The capability decision.</param>
    /// <param name="enforce">Whether explicit entitlements are enforced.</param>
    /// <returns>Whether the operation may proceed.</returns>
    public static bool Permits(AuthorizationDecision decision, bool enforce) => decision.IsAllowed
        || (!enforce && decision.Reason is AuthorizationReason.NotGranted or AuthorizationReason.NoPrincipal);

    /// <summary>Records a bounded, rate-limited audit without logging the subject.</summary>
    /// <param name="decision">The capability decision.</param>
    /// <param name="principal">The caller.</param>
    /// <param name="authentication">The authentication configuration.</param>
    /// <param name="loggers">The silo's logger factory.</param>
    /// <param name="grains">The observation grain factory, when available.</param>
    public static void Audit(AuthorizationDecision decision, ClaimsPrincipal? principal, Authentication authentication, ILoggerFactory loggers, IGrainFactory? grains = null)
    {
        if (!NeedsAudit(decision, authentication)) return;
        var holder = PrincipalKey.From(principal, authentication);
        var store = decision.Scope.EventStore?.Value ?? string.Empty;
        var key = (holder, store, decision.Scope.Namespace?.Value, decision.Capability.Value);
        if (!TakeSlot(loggers, key, DateTimeOffset.UtcNow)) return;
        Record(new(decision, holder, store, CallerKind(principal, authentication, holder)), loggers, grains);
    }

    /// <summary>Takes the slot of an audit key, unless it was recorded within the interval or the tracker is full of current entries.</summary>
    /// <param name="loggers">The logger factory that owns the tracker.</param>
    /// <param name="key">The audit key.</param>
    /// <param name="now">The current time.</param>
    /// <returns>Whether the caller should record the key.</returns>
    internal static bool TakeSlot(ILoggerFactory loggers, (PrincipalKey Holder, string Store, string? Namespace, string Capability) key, DateTimeOffset now)
    {
        var tracker = _trackers.GetValue(loggers, static _ => new());
        while (true)
        {
            var snapshot = Volatile.Read(ref tracker.LastSeen);
            if (snapshot.TryGetValue(key, out var seen) && now - seen < _interval) return false;
            if (!snapshot.ContainsKey(key) && snapshot.Count >= 10_000)
            {
                // Entries older than the interval can be recorded again anyway, so they give up their slots before the tracker is called full.
                var pruned = Prune(snapshot, now);
                if (pruned.Count < snapshot.Count)
                {
                    Interlocked.CompareExchange(ref tracker.LastSeen, pruned, snapshot);
                    continue;
                }

                if (Interlocked.Exchange(ref tracker.FullReported, 1) == 0) _ = Task.Run(loggers.CreateLogger(typeof(StoreCapabilityDecision).FullName!).StoreEntitlementAuditFull);
                return false;
            }
            if (ReferenceEquals(Interlocked.CompareExchange(ref tracker.LastSeen, snapshot.SetItem(key, now), snapshot), snapshot)) return true;
        }
    }

    static ImmutableDictionary<(PrincipalKey Holder, string Store, string? Namespace, string Capability), DateTimeOffset> Prune(
        ImmutableDictionary<(PrincipalKey Holder, string Store, string? Namespace, string Capability), DateTimeOffset> snapshot, DateTimeOffset now) =>
        snapshot.RemoveRange(snapshot.Where(entry => now - entry.Value >= _interval).Select(entry => entry.Key));

    static void Record(AuditEvidence evidence, ILoggerFactory loggers, IGrainFactory? grains)
    {
        var logger = loggers.CreateLogger(typeof(StoreCapabilityDecision).FullName!);
        _ = Task.Run(async () =>
        {
            try
            {
                var (decision, holder, store, callerKind) = evidence;
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(holder.Subject)))[..8];
                logger.StoreEntitlementAudited(decision.Capability.Value, decision.Reason.ToString(), store, callerKind, holder.Issuer, hash);
                if (grains is not null && !holder.IsUnidentified && decision.Scope.EventStore is not null)
                {
                    await grains.GetGrain<IStoreEntitlementObservations>(IStoreEntitlementObservations.Key).Record(holder, decision.Scope.EventStore, decision.Scope.Namespace, WellKnownCapabilities.AccessOf(decision.Capability));
                }
            }
            catch (Exception exception)
            {
                // Best-effort evidence must never affect a request or become authority.
                logger.StoreEntitlementObservationFailed(exception);
            }
        });
    }

    static bool NeedsAudit(AuthorizationDecision decision, Authentication authentication) => !authentication.EnforceStoreEntitlements
        && !decision.IsAllowed && decision.Reason is AuthorizationReason.NotGranted or AuthorizationReason.NoPrincipal;

    static string CallerKind(ClaimsPrincipal? principal, Authentication authentication, PrincipalKey holder)
    {
        if (PrincipalKey.IsLocalUser(principal, authentication)) return "user";
        return holder.IsUnidentified ? "anonymous" : "application";
    }

    sealed record AuditEvidence(AuthorizationDecision Decision, PrincipalKey Holder, string Store, string CallerKind);

    sealed class AuditComparer : IEqualityComparer<(PrincipalKey Holder, string Store, string? Namespace, string Capability)>
    {
        public bool Equals((PrincipalKey Holder, string Store, string? Namespace, string Capability) x, (PrincipalKey Holder, string Store, string? Namespace, string Capability) y) =>
            x.Holder == y.Holder && StringComparer.OrdinalIgnoreCase.Equals(x.Store, y.Store) && x.Namespace == y.Namespace && x.Capability == y.Capability;

        public int GetHashCode((PrincipalKey Holder, string Store, string? Namespace, string Capability) obj) =>
            HashCode.Combine(obj.Holder, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Store), obj.Namespace, obj.Capability);
    }

    sealed class AuditTracker
    {
        public ImmutableDictionary<(PrincipalKey Holder, string Store, string? Namespace, string Capability), DateTimeOffset> LastSeen = ImmutableDictionary.Create<(PrincipalKey Holder, string Store, string? Namespace, string Capability), DateTimeOffset>(new AuditComparer());
        public int FullReported;
    }
}
