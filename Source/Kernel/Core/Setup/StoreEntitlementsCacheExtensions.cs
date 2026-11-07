// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Setup;

/// <summary>Registers per-silo explicit grant snapshots and their broadcaster.</summary>
public static class StoreEntitlementsCacheExtensions
{
    /// <summary>Adds the per-silo entitlement cache and grain service.</summary>
    /// <param name="builder">The silo builder.</param>
    /// <returns>The builder for continuation.</returns>
    public static ISiloBuilder AddStoreEntitlementsCache(this ISiloBuilder builder)
    {
        builder.AddGrainService<StoreEntitlementsCacheGrainService>();
        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IStoreEntitlementsCache, StoreEntitlementsCache>();
            services.AddSingleton<IStoreEntitlementsCacheClient, StoreEntitlementsCacheClient>();
        });
        return builder;
    }
}
