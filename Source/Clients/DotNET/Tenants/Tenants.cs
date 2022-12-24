// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration.Grains;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenants"/>.
/// </summary>
public class Tenants : ITenants
{
    readonly IClusterClient _clusterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    public Tenants(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TenantId>> All() => _clusterClient.GetGrain<IConfiguration>(Guid.Empty).GetTenants();
}
