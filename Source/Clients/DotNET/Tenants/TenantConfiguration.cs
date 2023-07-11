// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
public class TenantConfiguration : ITenantConfiguration
{
    readonly IClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    public TenantConfiguration(IClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<ConfigurationForTenant> GetAllFor(TenantId tenantId)
    {
        var result = await _client.PerformQuery<Dictionary<string, string>>($"/api/configuration/tenants/{tenantId}");
        return new(result.Data);
    }
}
