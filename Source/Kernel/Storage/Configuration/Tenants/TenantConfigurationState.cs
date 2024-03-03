// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Configuration.Tenants;

/// <summary>
/// Represents all configuration key/values for a tenant.
/// </summary>
public class TenantConfigurationState : Dictionary<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationState"/> class.
    /// </summary>
    public TenantConfigurationState()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationState"/> class.
    /// </summary>
    /// <param name="dictionary">Dictionary to initialize it from.</param>
    public TenantConfigurationState(IDictionary<string, string> dictionary) : base(dictionary)
    {
    }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    /// <returns>A new empty <see cref="TenantConfigurationState"/>.</returns>
    public static TenantConfigurationState Empty() => new();
}
