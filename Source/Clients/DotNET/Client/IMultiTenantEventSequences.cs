// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines multi tenanted event sequences.
/// </summary>
public interface IMultiTenantEventSequences
{
    /// <summary>
    /// Gets the <see cref="IEventSequences"/> for a specific tenant.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> to get for.</param>
    /// <returns><see cref="IEventSequences"/>.</returns>
    IEventSequences ForTenant(TenantId tenantId);
}
