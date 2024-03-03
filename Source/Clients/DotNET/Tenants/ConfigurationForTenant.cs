// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Tenants;

/// <summary>
/// Represents all configuration key/values for a tenant.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigurationForTenant"/> class.
/// </remarks>
/// <param name="dictionary">Dictionary to initialize it from.</param>
public class ConfigurationForTenant(IDictionary<string, string> dictionary) : Dictionary<string, string>(dictionary)
{
}
