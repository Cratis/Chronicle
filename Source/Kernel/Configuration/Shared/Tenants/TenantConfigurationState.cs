// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Concepts.Configuration.Tenants;

/// <summary>
/// Represents all configuration key/values for a tenant.
/// </summary>
public class TenantConfigurationState : Dictionary<string, string>
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "tenant-configuration-state";

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
