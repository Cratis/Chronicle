// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Boot;
using Aksio.Cratis.Kernel.Configuration;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Configuration.Tenants;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for tenant configuration.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IGrainFactory _grainFactory;
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public BootProcedure(
        IGrainFactory grainFactory,
        KernelConfiguration configuration)
    {
        _grainFactory = grainFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        foreach (var (tenantId, tenant) in _configuration.Tenants)
        {
            var tenantConfiguration = _grainFactory.GetGrain<ITenantConfiguration>(Guid.Parse(tenantId));
            tenantConfiguration.Set(tenant.Configuration).Wait();
        }
    }
}
