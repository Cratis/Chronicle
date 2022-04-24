// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents all storage configuration for a tenant within a microservice.
/// </summary>
public class StorageForTenants : Dictionary<string, StorageTypes>
{
    /// <summary>
    /// Get <see cref="StorageTypes"/> for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <returns><see cref="StorageTypes"/> instance.</returns>
    public StorageTypes Get(TenantId tenantId) => this[tenantId.ToString()];
}
