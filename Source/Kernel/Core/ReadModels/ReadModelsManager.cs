// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Providers;

namespace Cratis.Chronicle.ReadModels;

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
        var readModels = State.ReadModels.ToList();
        foreach (var definition in definitions)
        {
            var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
            if (existing is not null)
            {
                readModels.Remove(existing);
            }

            readModels.Add(definition);
        }

        State.ReadModels = readModels;
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
        var readModels = State.ReadModels.ToList();
        var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
        if (existing is not null)
        {
            readModels.Remove(existing);
        }

        readModels.Add(definition);
        State.ReadModels = readModels;
        await WriteStateAsync();

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);
    }

    /// <inheritdoc/>
    public async Task UpdateDefinition(ReadModelDefinition definition)
    {
        var readModels = State.ReadModels.ToList();
        var existing = readModels.Find(_ => _.Identifier == definition.Identifier) ?? throw new ReadModelNotFound(definition.Identifier);
        readModels.Remove(existing);
        readModels.Add(definition);
        State.ReadModels = readModels;
        await WriteStateAsync();

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);
}
