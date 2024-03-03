// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Tenants;

/// <summary>
/// Defines a system for working with configuration for tenants.
/// </summary>
public interface ITenantConfiguration
{
    /// <summary>
    /// Get <see cref="ConfigurationForTenant"/> for a specific tenant.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> to get for.</param>
    /// <returns>The configuration for the tenant.</returns>
    Task<ConfigurationForTenant> GetAllFor(TenantId tenantId);
}
