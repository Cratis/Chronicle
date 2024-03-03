// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenants"/>.
/// </summary>
public class Tenants : ITenants
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tenants"/> class.
    /// </summary>
    public Tenants()
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> All()
    {
        // var result = await _connection.PerformQuery<IEnumerable<Tenant>>("/api/configuration/tenants");
        // return result.Data;
        await Task.CompletedTask;
        return null!;
    }
}
