// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
public class TenantConfiguration : ITenantConfiguration
{
    readonly IClusterClient _clusterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    /// <param name="clusterClient">The Orleans <see cref="IClusterClient"/>.</param>
    public TenantConfiguration(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public async Task<ConfigurationForTenant> GetAllFor(TenantId tenantId)
    {
        var grain = _clusterClient.GetGrain<Configuration.Grains.Tenants.ITenantConfiguration>(tenantId);
        var config = await grain.All();
        return new(config);
    }
}
