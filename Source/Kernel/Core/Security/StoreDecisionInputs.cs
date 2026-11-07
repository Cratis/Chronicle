// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Security;

/// <summary>Reuses immutable inputs and completed results, never a caller's authority.</summary>
internal sealed class StoreDecisionInputs
{
    readonly ConcurrentDictionary<(string Store, string? Namespace), AuthorizationScope> _scopes = new();
    readonly ConcurrentDictionary<(Capability Capability, AuthorizationScope Scope, AuthorizationReason Reason, bool Allowed), Task<AuthorizationDecision>> _results = new();

    /// <summary>Reuses a scope without retaining request state.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <returns>The immutable scope.</returns>
    internal AuthorizationScope Scope(string store, string? ns)
    {
        var key = (store, ns);
        if (_scopes.TryGetValue(key, out var scope)) return scope;
        scope = ns is null ? AuthorizationScope.ForEventStore(store) : AuthorizationScope.ForNamespace(store, ns);
        return _scopes.Count < 10_000 ? _scopes.GetOrAdd(key, scope) : scope;
    }

    /// <summary>Reuses a completed result after authority has been checked.</summary>
    /// <param name="reason">The freshly decided reason.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="allowed">Whether access is allowed.</param>
    /// <returns>The completed result.</returns>
    internal Task<AuthorizationDecision> Result(AuthorizationReason reason, Capability capability, AuthorizationScope scope, bool allowed)
    {
        var key = (capability, scope, reason, allowed);
        if (_results.TryGetValue(key, out var result)) return result;
        result = Task.FromResult(new AuthorizationDecision(allowed ? AuthorizationOutcome.Allowed : AuthorizationOutcome.Denied, reason, capability, scope));
        return _results.Count < 10_000 ? _results.GetOrAdd(key, result) : result;
    }
}
