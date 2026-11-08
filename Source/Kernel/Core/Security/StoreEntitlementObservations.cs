// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>Retains at most 5,000 observation identities; evidence expires on reactivation or restart.</summary>
public class StoreEntitlementObservations : Grain, IStoreEntitlementObservations
{
    const int Limit = 5_000;
    readonly Dictionary<(PrincipalKey, EventStoreName, EventStoreNamespaceName?, StoreAccess), StoreEntitlementObservation> _observations = [];

    /// <inheritdoc/>
    public Task Record(PrincipalKey holder, EventStoreName eventStore, EventStoreNamespaceName? @namespace, StoreAccess access)
    {
        if (holder.IsUnidentified || SystemStoreAccess.IsSystem(eventStore)) return Task.CompletedTask;
        var key = (holder, eventStore, @namespace, access);
        var now = DateTimeOffset.UtcNow;
        if (_observations.TryGetValue(key, out var previous))
        {
            _observations[key] = previous with { LastSeen = now, Count = previous.Count + 1 };
        }
        else
        {
            if (_observations.Count >= Limit) _observations.Remove(_observations.MinBy(entry => entry.Value.LastSeen).Key);
            _observations[key] = new(holder, eventStore, @namespace, access, now, now, 1);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<StoreEntitlementObservation>> GetSnapshot() => Task.FromResult<IEnumerable<StoreEntitlementObservation>>(_observations.Values.ToArray());
}
