// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Shared.Configuration;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Configuration;

/// <summary>
/// Defines a system for working with configurations.
/// </summary>
public interface IConfiguration : IGrainWithGuidKey
{
    /// <summary>
    /// Get all the configuration tenants.
    /// </summary>
    /// <returns>Collection of <see cref="TenantId"/>.</returns>
    Task<IEnumerable<TenantId>> GetTenants();

    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    /// <returns><see cref="Storage"/> configuration instance.</returns>
    Task<StorageForMicroservice> GetStorage();
}
