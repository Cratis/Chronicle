// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>An immutable, local index of explicit grants.</summary>
/// <remarks>Initializes a new instance of the <see cref="StoreEntitlementsSnapshot"/> class.</remarks>
/// <param name="state">The authoritative grants.</param>
public sealed class StoreEntitlementsSnapshot(StoreEntitlementsState state)
{
    readonly FrozenDictionary<(PrincipalKey Holder, string? Store, string? Namespace), StoreAccess> _entries = Index(state);

    /// <summary>Gets an unloaded snapshot.</summary>
    public static StoreEntitlementsSnapshot Empty { get; } = new(new(0, []));

    /// <summary>Gets the version of the indexed grants.</summary>
    public long Version { get; } = state.Version;

    /// <summary>Gets reusable immutable inputs for this snapshot.</summary>
    internal StoreDecisionInputs Inputs { get; } = new();

    /// <summary>Tests only explicit grants, without consulting any remote service.</summary>
    /// <param name="holder">The issuer-qualified principal.</param>
    /// <param name="store">The addressed store.</param>
    /// <param name="ns">The addressed namespace.</param>
    /// <param name="access">All required access flags.</param>
    /// <returns>Whether the grant covers the scope and all requested access.</returns>
    public bool Allows(PrincipalKey holder, EventStoreName store, EventStoreNamespaceName? ns, StoreAccess access)
    {
        if (SystemStoreAccess.IsSystem(store) || access == StoreAccess.None) return false;
        _entries.TryGetValue((holder, store.Value, ns?.Value), out var granted);
        _entries.TryGetValue((holder, store.Value, null), out var storeGrant);
        _entries.TryGetValue((holder, null, ns?.Value), out var namespaceGrant);
        _entries.TryGetValue((holder, null, null), out var globalGrant);
        return (granted | storeGrant | namespaceGrant | globalGrant).HasFlag(access);
    }

    static FrozenDictionary<(PrincipalKey Holder, string? Store, string? Namespace), StoreAccess> Index(StoreEntitlementsState state)
    {
        var entries = new Dictionary<(PrincipalKey Holder, string? Store, string? Namespace), StoreAccess>(new GrantComparer());
        foreach (var grant in state.Grants)
        {
            var key = (grant.Holder, grant.EventStore?.Value, grant.Namespace?.Value);
            entries.TryGetValue(key, out var access);
            entries[key] = access | grant.Access;
        }
        return entries.ToFrozenDictionary(entries.Comparer);
    }

    sealed class GrantComparer : IEqualityComparer<(PrincipalKey Holder, string? Store, string? Namespace)>
    {
        public bool Equals((PrincipalKey Holder, string? Store, string? Namespace) x, (PrincipalKey Holder, string? Store, string? Namespace) y) =>
            x.Holder == y.Holder && StringComparer.OrdinalIgnoreCase.Equals(x.Store, y.Store) && StringComparer.Ordinal.Equals(x.Namespace, y.Namespace);

        public int GetHashCode((PrincipalKey Holder, string? Store, string? Namespace) obj) =>
            HashCode.Combine(obj.Holder, obj.Store is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Store), obj.Namespace);
    }
}
