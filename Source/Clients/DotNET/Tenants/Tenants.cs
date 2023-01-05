// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenants"/>.
/// </summary>
public class Tenants : ITenants
{
    readonly IClient _client;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    public Tenants(IClient client, JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> All()
    {
        var result = await _client.PerformQuery("/api/configuration/tenants");
        var element = (JsonElement)result.Data;
        return element.Deserialize<IEnumerable<Tenant>>(_jsonSerializerOptions)!;
    }
}
