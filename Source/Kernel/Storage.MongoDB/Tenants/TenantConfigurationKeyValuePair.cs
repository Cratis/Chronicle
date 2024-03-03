// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Storage.MongoDB.Tenants;

/// <summary>
/// Represents the key / value pair for tenant configuration.
/// </summary>
/// <param name="Key">Key of configuration pair.</param>
/// <param name="Value">Value for the configuration pair. </param>
public record TenantConfigurationKeyValuePair(string Key, string Value);
