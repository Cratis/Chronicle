// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenants"/>.
/// </summary>
public class Tenants : ITenants
{
    readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    /// <param name="connection">The Cratis <see cref="IConnection"/>.</param>
    public Tenants(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> All()
    {
        var result = await _connection.PerformQuery<IEnumerable<Tenant>>("/api/configuration/tenants");
        return result.Data;
    }
}
