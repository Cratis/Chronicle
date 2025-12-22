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
        State.ReadModels = definitions.ToList();
        await WriteStateAsync();

        foreach (var definition in definitions)
        {
            var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
            await readModelGrain.SetDefinition(definition);
        }
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(ReadModelDefinition definition)
    {
        var existing = State.ReadModels.FirstOrDefault(_ => _.Identifier == definition.Identifier);
        if (existing is not null)
        {
            State.ReadModels.Remove(existing);
        }

        State.ReadModels.Add(definition);
        await WriteStateAsync();

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);
    }

    /// <inheritdoc/>
    public async Task UpdateDefinition(ReadModelDefinition definition)
    {
        var existing = State.ReadModels.FirstOrDefault(_ => _.Identifier == definition.Identifier);
        if (existing is null)
        {
            throw new ReadModelNotFound(definition.Identifier);
        }

        State.ReadModels.Remove(existing);
        State.ReadModels.Add(definition);
        await WriteStateAsync();

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);
}
