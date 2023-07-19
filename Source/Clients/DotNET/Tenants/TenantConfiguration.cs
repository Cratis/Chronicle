// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
public class TenantConfiguration : ITenantConfiguration
{
    readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    /// <param name="connection">The Cratis <see cref="IConnection"/>.</param>
    public TenantConfiguration(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<ConfigurationForTenant> GetAllFor(TenantId tenantId)
    {
        var result = await _connection.PerformQuery<Dictionary<string, string>>($"/api/configuration/tenants/{tenantId}");
        return new(result.Data);
    }
}
