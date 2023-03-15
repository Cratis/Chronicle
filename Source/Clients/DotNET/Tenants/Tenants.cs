// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenants"/>.
/// </summary>
public class Tenants : ITenants
{
    readonly IClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    public Tenants(IClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> All()
    {
        var result = await _client.PerformQuery<IEnumerable<Tenant>>("/api/configuration/tenants");
        return result.Data;
    }
}
