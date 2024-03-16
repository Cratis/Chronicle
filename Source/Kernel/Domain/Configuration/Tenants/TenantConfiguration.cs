// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Configuration.Tenants;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Domain.Configuration.Tenants;

/// <summary>
/// Represents the API for working with configuration related to specific tenants.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
/// </remarks>
/// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
[Route("/api/configuration/tenants/{tenantId}")]
public class TenantConfiguration(IGrainFactory grainFactory) : ControllerBase
{
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
        var tenantConfiguration = grainFactory.GetGrain<ITenantConfiguration>(tenantId);
        await tenantConfiguration.Set(keyValuePair.Key, keyValuePair.Value);
    }
}
