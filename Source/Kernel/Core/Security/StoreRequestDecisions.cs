// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Security;

/// <summary>Reuses the last ordinary-store decision within a request, never across snapshot or lease changes.</summary>
internal sealed class StoreRequestDecisions
{
    static readonly ConditionalWeakTable<HttpContext, StoreRequestDecisions> _requests = new();
    readonly object _sync = new();
    Entry? _entry;

    /// <summary>Gets the bounded request-local cache.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The cache.</returns>
    internal static StoreRequestDecisions For(HttpContext request) => _requests.GetValue(request, static _ => new());

    /// <summary>Finds a decision still valid for the published snapshot and lease.</summary>
    /// <param name="key">The caller and addressed scope.</param>
    /// <param name="cache">The current entitlements.</param>
    /// <param name="authentication">The current policy.</param>
    /// <returns>The reusable decision, if any.</returns>
    internal AuthorizationDecision? Find((ClaimsPrincipal? Principal, string Capability, string Store, string? Namespace) key, IStoreEntitlementsCache cache, Authentication authentication)
    {
        lock (_sync)
        {
            var entry = _entry;
            return entry is not null && entry.Key == key
                && ReferenceEquals(entry.Version.Snapshot, cache.Snapshot) && entry.Version.Fresh == cache.IsFresh
                && ReferenceEquals(entry.Policy.Instance, authentication) && entry.Policy.Enabled == authentication.Enabled && entry.Policy.Enforce == authentication.EnforceStoreEntitlements
                ? entry.Decision : null;
        }
    }

    /// <summary>Remembers the decision and the authority it was based on.</summary>
    /// <param name="key">The caller and addressed scope.</param>
    /// <param name="version">The snapshot and lease state used.</param>
    /// <param name="authentication">The current policy.</param>
    /// <param name="decision">The decision.</param>
    internal void Remember((ClaimsPrincipal? Principal, string Capability, string Store, string? Namespace) key, (StoreEntitlementsSnapshot Snapshot, bool Fresh) version, Authentication authentication, AuthorizationDecision decision)
    {
        lock (_sync)
        {
            _entry = new(key, version, (authentication, authentication.Enabled, authentication.EnforceStoreEntitlements), decision);
        }
    }

    /// <summary>Limits repeated audit work within the same request.</summary>
    /// <param name="decision">The decision.</param>
    /// <returns>Whether audit work is due.</returns>
    internal bool TakeAudit(AuthorizationDecision decision)
    {
        lock (_sync)
        {
            if (_entry is not { } entry || !ReferenceEquals(entry.Decision, decision)) return true;
            var now = DateTimeOffset.UtcNow;
            if (now - entry.AuditedAt < TimeSpan.FromMinutes(1)) return false;
            entry.AuditedAt = now;
            return true;
        }
    }

    sealed record Entry((ClaimsPrincipal? Principal, string Capability, string Store, string? Namespace) Key, (StoreEntitlementsSnapshot Snapshot, bool Fresh) Version, (Authentication Instance, bool Enabled, bool Enforce) Policy, AuthorizationDecision Decision)
    {
        /// <summary>Gets or sets the last audit attempt within this request.</summary>
        internal DateTimeOffset AuditedAt { get; set; }
    }
}
