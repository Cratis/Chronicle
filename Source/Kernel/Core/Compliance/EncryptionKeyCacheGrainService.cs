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
    public Task Evict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (keyStore is IEvictEncryptionKeyCache evictable)
        {
            evictable.EvictFromCache(eventStore, eventStoreNamespace, identifier);
        }

        return Task.CompletedTask;
    }
}
