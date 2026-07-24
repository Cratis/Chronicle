// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Defines a client that evicts an encryption key from the storage cache on every silo in the cluster.
/// </summary>
public interface IEncryptionKeyCacheClient
{
    /// <summary>
    /// Evict an encryption key from the storage cache on every silo in the cluster.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> to evict.</param>
    /// <returns>Awaitable task that completes once every silo has acknowledged the eviction.</returns>
    Task Evict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
