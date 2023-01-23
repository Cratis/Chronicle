// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
public class TenantConfiguration : ITenantConfiguration
{
    readonly IClient _client;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfiguration"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    public TenantConfiguration(IClient client, JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<ConfigurationForTenant> GetAllFor(TenantId tenantId)
    {
        var result = await _client.PerformQuery("/api/configuration/tenants/{tenantId}");
        var element = (JsonElement)result.Data;
        return element.Deserialize<ConfigurationForTenant>(_jsonSerializerOptions)!;
    }
}
