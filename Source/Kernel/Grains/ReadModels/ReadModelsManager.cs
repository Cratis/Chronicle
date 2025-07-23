// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsManager"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModelsManager)]
public class ReadModelsManager : Grain<ReadModelsManagerState>, IReadModelsManager
{
    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ReadModelDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            var readModelGrain = GrainFactory.GetGrain<IReadModel>(definition.Name);
            await readModelGrain.SetDefinition(definition);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);
}
