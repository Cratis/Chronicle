// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration for a specific tenant.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Gets the name of the tenant.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the configuration key/value pairs associated with the tenant.
    /// </summary>
    public IDictionary<string, string> Configuration { get; init; } = new Dictionary<string, string>();
}
