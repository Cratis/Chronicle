// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModel"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModels)]
public class ReadModel : Grain<ReadModelDefinition>, IReadModel
{
    /// <inheritdoc/>
    public async Task SetDefinition(ReadModelDefinition definition)
    {
        State = definition;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<ReadModelDefinition> GetDefinition() => Task.FromResult(State);
}
