// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for a specific tenant.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Gets the name of the tenant.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
