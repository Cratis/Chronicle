// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents all the configured tenants.
/// </summary>
public class Tenants : Dictionary<string, Tenant>
{
    /// <summary>
    /// Get all <see cref="TenantId">tenant ids</see> configured.
    /// </summary>
    /// <returns>Collection of <see cref="TenantId"/>.</returns>
    public IEnumerable<TenantId> GetTenantIds() => Keys.Select(_ => (TenantId)_).ToArray();
}
