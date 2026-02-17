// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Orleans.Providers;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an implementation of <see cref="IDataProtectionKeys"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.DataProtectionKeys)]
public class DataProtectionKeys : Grain<DataProtectionKeysState>, IDataProtectionKeys
{
    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetAllKeys() =>
        Task.FromResult(State.Keys.Select(k => k.Xml));

    /// <inheritdoc/>
    public async Task StoreKey(string friendlyName, string xml)
    {
        var key = new DataProtectionKey(
            Guid.NewGuid().ToString(),
            friendlyName,
            xml,
            DateTimeOffset.UtcNow);

        State.NewKeys.Add(key);
        await WriteStateAsync();
    }
}
