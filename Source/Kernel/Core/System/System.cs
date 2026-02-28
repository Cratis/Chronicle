// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Orleans.Providers;

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Represents an implementation of <see cref="ISystem"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.System)]
public class System : Grain<SystemInformationState>, ISystem
{
    /// <inheritdoc/>
    public Task<SemanticVersion?> GetVersion() => Task.FromResult(State.Version);

    /// <inheritdoc/>
    public async Task SetVersion(SemanticVersion version)
    {
        State.Version = version;
        await WriteStateAsync();
    }
}
