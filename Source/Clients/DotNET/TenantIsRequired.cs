// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis;

/// <summary>
/// Exception that gets thrown when a tenant is required for a multi tenanted client.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TenantIsRequired"/> class.
/// </remarks>
/// <param name="action">Specific action that is failing.</param>
public class TenantIsRequired(string action) : Exception($"Tenant is required for a multi tenanted client when {action}")
{
}
