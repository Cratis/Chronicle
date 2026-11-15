// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Security;

/// <summary>Publishes authoritative grants on its own silo.</summary>
/// <param name="grainId">The service identity.</param>
/// <param name="silo">The owning silo.</param>
/// <param name="cache">The local snapshot cache.</param>
/// <param name="loggerFactory">The logger factory.</param>
[Reentrant]
public class StoreEntitlementsCacheGrainService(GrainId grainId, Silo silo, IStoreEntitlementsCache cache, ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IStoreEntitlementsCacheGrainService
{
    /// <inheritdoc/>
    public Task Refresh(StoreEntitlementsState state)
    {
        cache.Publish(state);
        return Task.CompletedTask;
    }
}
