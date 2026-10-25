// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings;

/// <summary>
/// Represents the in memory implementation of <see cref="IObserverIdentityClaims"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> holding the observer definitions.</param>
public sealed class InMemoryObserverIdentityClaims(IStorage storage) : IObserverIdentityClaims
{
    readonly object _lock = new();
    readonly Dictionary<(EventStoreName Store, ObserverId ObserverId), ObserverIdentityClaim> _claims = [];

    /// <inheritdoc/>
    public Task<ObserverIdentityClaim> Claim(EventStoreName store, ObserverIdentityClaim claim)
    {
        lock (_lock)
        {
            _claims.TryAdd((store, claim.ObserverId), claim);
            return Task.FromResult(_claims[(store, claim.ObserverId)]);
        }
    }

    /// <inheritdoc/>
    public Task<ObserverIdentityClaim?> Get(EventStoreName store, ObserverId observerId)
    {
        lock (_lock)
        {
            return Task.FromResult(_claims.GetValueOrDefault((store, observerId)));
        }
    }

    /// <inheritdoc/>
    public Task<bool> Release(EventStoreName store, ObserverIdentityClaim claim)
    {
        lock (_lock)
        {
            if (!_claims.TryGetValue((store, claim.ObserverId), out var stored) || stored != claim) return Task.FromResult(false);
            return Task.FromResult(_claims.Remove((store, claim.ObserverId)));
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasDefinition(EventStoreName store, ObserverId observerId) => storage.GetEventStore(store).Observers.Has(observerId);
}
