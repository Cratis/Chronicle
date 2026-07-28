// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Orleans.Services;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Defines a service that lives in each silo and evicts that silo's encryption key cache for a specific identifier,
/// so a crypto-shredded key stops decrypting on every silo in the cluster.
/// </summary>
public interface IEncryptionKeyCacheGrainService : IGrainService
{
    /// <summary>
    /// Evict the local silo's cache for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> to evict.</param>
    /// <returns>Awaitable task.</returns>
    Task Evict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
