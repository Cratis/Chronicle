// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
public class TenantConfiguration : ITenantConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    public TenantConfiguration()
    {
    }

    /// <inheritdoc/>
    public async Task<ConfigurationForTenant> GetAllFor(TenantId tenantId)
    {
        // var result = await _connection.PerformQuery<Dictionary<string, string>>($"/api/configuration/tenants/{tenantId}");
        // return new(result.Data);
        await Task.CompletedTask;
        return null!;
    }
}
