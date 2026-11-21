// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyCacheGrainService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EncryptionKeyCacheGrainService"/> class.
/// </remarks>
/// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
/// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
/// <param name="keyStore">The local silo's <see cref="IEncryptionKeyStorage"/> whose cache is evicted.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Reentrant]
public class EncryptionKeyCacheGrainService(
    GrainId grainId,
    Silo silo,
    IEncryptionKeyStorage keyStore,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IEncryptionKeyCacheGrainService
{
    /// <inheritdoc/>
    public Task Evict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        EvictFor(keyStore, EnvelopeSigningKeys.Shared, new(eventStore, eventStoreNamespace, identifier));

    /// <summary>Invalidates local key caches, revoking only writes covered by the freshly read durable fence.</summary>
    /// <param name="keyStore">The local key store.</param>
    /// <param name="signingKeys">The signing-key cache.</param>
    /// <param name="address">The key-storage address.</param>
    /// <returns>The cache invalidation task.</returns>
    internal static async Task EvictFor(IEncryptionKeyStorage keyStore, EnvelopeSigningKeys signingKeys, ProtectedValueAddress address)
    {
        var commitment = EnvelopeSigning.Commit(address);
        try
        {
            if (keyStore is IEvictEncryptionKeyCache evictable)
            {
                evictable.EvictFromCache(address.EventStore, address.Namespace, address.Identifier);
            }

            // Authorization uses the same grain method as erasure. Its live successor is not covered by this fence.
            var erasure = await keyStore.GetErasureForRelease(address.EventStore, address.Namespace, address.Identifier);
            signingKeys.Evict(commitment, erasure);
        }
        catch
        {
            // Even when storage is unavailable, erasure must dispose cached signers and prevent stale retries.
            signingKeys.Evict(commitment, revokeAll: true);
            throw;
        }
    }
}
