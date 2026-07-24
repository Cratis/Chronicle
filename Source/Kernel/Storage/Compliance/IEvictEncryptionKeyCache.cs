// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Defines an encryption key store that keeps a local in-memory cache which can be evicted for a specific identifier.
/// </summary>
/// <remarks>
/// This is the local-silo eviction primitive used by cluster-wide crypto-shred: the fan-out reaches every silo and
/// each silo evicts its own cache through this interface. It only clears the local cache; it neither touches the
/// backing store nor fans out further.
/// </remarks>
public interface IEvictEncryptionKeyCache
{
    /// <summary>
    /// Evict every cached entry - both present keys and remembered absences - for an identifier from the local cache.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> to evict.</param>
    void EvictFromCache(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
