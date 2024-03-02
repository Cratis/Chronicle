// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.Configuration.Tenants;

namespace Aksio.Cratis.Kernel.Grains.Configuration.Tenants;

/// <summary>
/// Defines a system for working with configuration for a specific tenant.
/// </summary>
public interface ITenantConfiguration : IGrainWithGuidKey
{
    /// <summary>
    /// Set a configuration key / value pair.
    /// </summary>
    /// <param name="key">Key to set.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Awaitable task.</returns>
    Task Set(string key, string value);

    /// <summary>
    /// Set a collection of configuration key / value pairs.
    /// </summary>
    /// <param name="collection">Dictionary with key/value pairs.</param>
    /// <returns>Awaitable task.</returns>
    Task Set(IDictionary<string, string> collection);

    /// <summary>
    /// Gets all the configuration for the tenant.
    /// </summary>
    /// <returns><see cref="TenantConfigurationState"/>.</returns>
    Task<IDictionary<string, string>> All();
}
