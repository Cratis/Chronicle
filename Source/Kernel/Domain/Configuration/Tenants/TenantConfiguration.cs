// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Configuration.Tenants;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Aksio.Cratis.Kernel.Domain.Configuration.Tenants;

/// <summary>
/// Represents the API for working with configuration related to specific tenants.
/// </summary>
[Route("/api/configuration/tenants/{tenantId}")]
public class TenantConfiguration : Controller
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    public TenantConfiguration(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Returns all the configuration key/value pairs associated with a specific tenant.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> for the tenant to get for.</param>
    /// <returns><see cref="IDictionary{TKey, TValue}"/> with all the key/value pairs.</returns>
    [HttpGet]
    public async Task<IDictionary<string, string>> AllConfigurationValuesForTenant([FromRoute] TenantId tenantId)
    {
        var tenantConfiguration = _grainFactory.GetGrain<ITenantConfiguration>(tenantId);
        return await tenantConfiguration.All();
    }

    /// <summary>
    /// Set a key/value pair configuration for a specific tenant.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> for the tenant to set for.</param>
    /// <param name="keyValuePair">The key value pair to set.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task SetConfigurationValueForTenant(
        [FromRoute] TenantId tenantId,
        [FromBody] KeyValuePair<string, string> keyValuePair)
    {
        var tenantConfiguration = _grainFactory.GetGrain<ITenantConfiguration>(tenantId);
        await tenantConfiguration.Set(keyValuePair.Key, keyValuePair.Value);
    }
}
