// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Configuration.Grains.Tenants;

/// <summary>
/// Defines a system for working with configuration for a specific tenant.
/// </summary>
public interface ITenantConfiguration : IGrainWithGuidKey
{
    /// <summary>
    /// Gets all the configuration for the tenant.
    /// </summary>
    /// <returns><see cref="TenantConfigurationState"/>.</returns>
    Task<TenantConfigurationState> All();
}
