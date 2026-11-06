// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public static void Audit(AuthorizationDecision decision, ClaimsPrincipal? principal, Authentication authentication, ILoggerFactory loggers)
    {
        if (!NeedsAudit(decision, authentication)) return;
        var holder = PrincipalKey.From(principal, authentication);
        var store = decision.Scope.EventStore?.Value ?? string.Empty;
        var key = (holder, store.ToUpperInvariant(), decision.Capability.Value);
        var logger = loggers.CreateLogger(typeof(StoreCapabilityDecision).FullName!);
        if (!TakeSlot(_trackers.GetValue(loggers, static _ => new()), key, logger)) return;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(holder.Subject)))[..8];
        logger.StoreEntitlementAudited(decision.Capability.Value, decision.Reason.ToString(), store, CallerKind(principal, authentication, holder), holder.Issuer, hash);
    }

    static bool NeedsAudit(AuthorizationDecision decision, Authentication authentication) => !authentication.EnforceStoreEntitlements
        && !decision.IsAllowed && decision.Reason is AuthorizationReason.NotGranted or AuthorizationReason.NoPrincipal;

    static bool TakeSlot(AuditTracker tracker, (PrincipalKey Holder, string Store, string Capability) key, ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;
        lock (tracker.LastSeen)
        {
            if (tracker.LastSeen.TryGetValue(key, out var seen))
            {
                if (now - seen < _interval) return false;
            }
            else if (tracker.LastSeen.Count >= 10_000)
            {
                if (!tracker.FullReported) logger.StoreEntitlementAuditFull();
                tracker.FullReported = true;
                return false;
            }
            tracker.LastSeen[key] = now;
            return true;
        }
    }

    static string CallerKind(ClaimsPrincipal? principal, Authentication authentication, PrincipalKey holder)
    {
        if (PrincipalKey.IsLocalUser(principal, authentication)) return "user";
        return holder.IsUnidentified ? "anonymous" : "application";
    }

    sealed class AuditTracker
    {
        public Dictionary<(PrincipalKey Holder, string Store, string Capability), DateTimeOffset> LastSeen { get; } = [];
        public bool FullReported { get; set; }
    }
}
