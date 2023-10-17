// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Configuration.Tenants;

/// <summary>
/// Represents the API for working with tenants.
/// </summary>
[Route("/api/configuration/tenants")]
public class Tenants : ControllerBase
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    public Tenants(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Get all the tenants.
    /// </summary>
    /// <returns>Collection of <see cref="TenantInfo"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<TenantInfo>> AllTenants()
    {
        var configuration = _grainFactory.GetGrain<IConfiguration>(Guid.Empty);
        return (await configuration.GetTenants()).ToArray();
    }
}
