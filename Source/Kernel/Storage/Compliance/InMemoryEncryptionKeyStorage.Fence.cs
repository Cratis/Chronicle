// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

public partial class InMemoryEncryptionKeyStorage
{
    readonly Dictionary<Scope, EncryptionKeyFenceState> _externalFences = [];

    /// <inheritdoc/>
    public bool IsDurable => false;

    /// <inheritdoc/>
    public Task<EncryptionKeyFenceState> ReadFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            if (!_externalFences.TryGetValue(scope, out var state)) _externalFences[scope] = state = EncryptionKeyFenceState.Empty;
            return Task.FromResult(state);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CompareExchangeFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next)
    {
        await EncryptionKeyRequestArchive.ValidateTransition(this, eventStore, eventStoreNamespace, identifier, observed, next);
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            if (!_externalFences.TryGetValue(scope, out var current) || current.Erasure.Version != observed.Erasure.Version) return false;
            _externalFences[scope] = next;
            return true;
        }
    }
}
