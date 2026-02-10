// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Settings;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Settings;

/// <summary>
/// Represents an implementation of <see cref="ISettings"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Settings)]
public class Settings : Grain<SettingsState>, ISettings
{
    /// <inheritdoc/>
    public async Task SetLanguageModelProvider(LanguageModelProvider provider)
    {
        State.LanguageModelProvider = provider;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<LanguageModelProvider> GetLanguageModelProvider() => Task.FromResult(State.LanguageModelProvider);
}
