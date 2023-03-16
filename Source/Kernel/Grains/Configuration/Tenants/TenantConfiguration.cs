// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Configuration.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantConfiguration"/>.
/// </summary>
[StorageProvider(ProviderName = TenantConfigurationState.StorageProvider)]
public class TenantConfiguration : Grain<TenantConfigurationState>, ITenantConfiguration
{
    /// <inheritdoc/>
    public async Task Set(string key, string value)
    {
        State[key] = value;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Set(IDictionary<string, string> collection)
    {
        foreach (var (key, value) in collection)
        {
            State[key] = value;
        }
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IDictionary<string, string>> All() => Task.FromResult<IDictionary<string,string>>(new Dictionary<string, string>(State));
}
