// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Security;

/// <summary>Fans out authoritative snapshots, leaving missed deliveries to the background poll.</summary>
/// <param name="grains">The management grain factory.</param>
/// <param name="services">The service provider.</param>
/// <param name="logger">The broadcast logger.</param>
[IgnoreConvention]
public class StoreEntitlementsCacheClient(IGrainFactory grains, IServiceProvider services, ILogger<StoreEntitlementsCacheClient> logger) : GrainServiceClient<IStoreEntitlementsCacheGrainService>(services), IStoreEntitlementsCacheClient
{
    /// <inheritdoc/>
    public async Task Refresh(StoreEntitlementsState state)
    {
        try
        {
            var hosts = await grains.GetGrain<IManagementGrain>(1).GetHosts(true);
            await Task.WhenAll(hosts.Keys.Select(async host =>
            {
                try { await GetGrainService(host).Refresh(state); }
                catch (Exception) { logger.StoreEntitlementsBroadcastFailed(); }
            }));
        }
        catch (Exception) { logger.StoreEntitlementsBroadcastFailed(); }
    }
}
