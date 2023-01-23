// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Defines a system for working with tenants.
/// </summary>
public interface ITenants
{
    /// <summary>
    /// Gets all the configured tenants from the Kernel.
    /// </summary>
    /// <returns>Collection of <see cref="TenantId"/>.</returns>
    Task<IEnumerable<Tenant>> All();
}
