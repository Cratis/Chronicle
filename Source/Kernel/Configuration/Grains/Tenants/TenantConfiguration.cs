// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts.Configuration.Tenants;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Concepts.Grains.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
[StorageProvider(ProviderName = TenantConfigurationState.StorageProvider)]
public class TenantConfiguration : Grain<TenantConfigurationState>, ITenantConfiguration
{
    /// <inheritdoc/>
    public Task<TenantConfigurationState> All() => Task.FromResult(State);
}
