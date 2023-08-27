// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Exception that gets thrown when a tenant is required for a multi tenanted client.
/// </summary>
public class TenantIsRequired : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsRequired"/> class.
    /// </summary>
    /// <param name="action">Specific action that is failing.</param>
    public TenantIsRequired(string action) : base($"Tenant is required for a multi tenanted client when {action}")
    {
    }
}
