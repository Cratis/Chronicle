// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents all configuration key/values for a tenant.
/// </summary>
public class ConfigurationForTenant : Dictionary<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationForTenant"/> class.
    /// </summary>
    /// <param name="dictionary">Dictionary to initialize it from.</param>
    public ConfigurationForTenant(IDictionary<string, string> dictionary) : base(dictionary)
    {
    }
}
