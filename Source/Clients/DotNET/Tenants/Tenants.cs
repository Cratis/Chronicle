// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

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
    public Task<IEnumerable<TenantId>> All() => throw new NotImplementedException();
}
