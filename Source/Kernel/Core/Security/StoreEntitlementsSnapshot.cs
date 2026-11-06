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
    readonly FrozenDictionary<PrincipalKey, Entry[]> _entries = state.Grants.GroupBy(grant => grant.Holder).ToFrozenDictionary(group => group.Key, group => group.Select(grant => new Entry(grant.EventStore, grant.Namespace, grant.Access)).ToArray());

    /// <summary>Gets an unloaded snapshot.</summary>
    public static StoreEntitlementsSnapshot Empty { get; } = new(new(0, []));

    /// <summary>Gets the version of the indexed grants.</summary>
    public long Version { get; } = state.Version;

    /// <summary>Tests only explicit grants, without consulting any remote service.</summary>
    /// <param name="holder">The issuer-qualified principal.</param>
    /// <param name="store">The addressed store.</param>
    /// <param name="ns">The addressed namespace.</param>
    /// <param name="access">All required access flags.</param>
    /// <returns>Whether the grant covers the scope and all requested access.</returns>
    public bool Allows(PrincipalKey holder, EventStoreName store, EventStoreNamespaceName? ns, StoreAccess access)
    {
        if (SystemStoreAccess.IsSystem(store) || access == StoreAccess.None || !_entries.TryGetValue(holder, out var entries)) return false;
        var granted = StoreAccess.None;
        foreach (var entry in entries)
        {
            if (entry.Store is not null && !StringComparer.OrdinalIgnoreCase.Equals(entry.Store.Value, store.Value)) continue;
            if (entry.Namespace is not null && entry.Namespace != ns) continue;
            granted |= entry.Access;
        }
        return granted.HasFlag(access);
    }

    sealed record Entry(EventStoreName? Store, EventStoreNamespaceName? Namespace, StoreAccess Access);
}
