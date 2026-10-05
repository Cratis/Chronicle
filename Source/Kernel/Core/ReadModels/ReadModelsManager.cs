// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Orleans.Providers;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsManager"/>.
/// </summary>
/// <param name="projectionPipelines"><see cref="IProjectionPipelineManager"/> for evicting pipelines left bound to a read model's former definition.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModelsManager)]
public class ReadModelsManager(IProjectionPipelineManager projectionPipelines) : Grain<ReadModelsManagerState>, IReadModelsManager
{
    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ReadModelDefinition> definitions)
    {
        var readModels = State.ReadModels.ToList();
        var changed = new List<ReadModelDefinition>();
        foreach (var definition in definitions)
        {
            var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
            if (existing is not null)
            {
                readModels.Remove(existing);
                if (existing != definition)
                {
                    changed.Add(definition);
                }
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

        foreach (var definition in changed)
        {
            await EvictProjectionsTargeting(definition.Identifier);
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

        if (existing is not null && existing != definition)
        {
            await EvictProjectionsTargeting(definition.Identifier);
        }
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

        if (existing != definition)
        {
            await EvictProjectionsTargeting(definition.Identifier);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);

    /// <summary>
    /// Evicts the cached pipeline of every projection that targets a read model whose definition just
    /// changed, so the next event delivered to it rebuilds its pipeline - and the sink bound to it -
    /// from the definition just written, rather than the one in effect when the pipeline was first built.
    /// </summary>
    /// <remarks>
    /// A projection's own re-registration is a no-op whenever its field mappings are unchanged, even
    /// when the read model it targets changed underneath it - re-registering a client's unchanged
    /// projection logic every reconnect is the common case this exists to keep free, and the projection
    /// definition carries no copy of the read model's own definition to compare against. Nothing else
    /// in that path learns that this read model's definition changed, so without this call a pipeline
    /// built against a superseded container name (or any other change in the read model's definition,
    /// such as a different naming policy) keeps writing to it until the silo restarts.
    /// </remarks>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> whose definition changed.</param>
    async Task EvictProjectionsTargeting(ReadModelIdentifier readModel)
    {
        var eventStore = (EventStoreName)this.GetPrimaryKeyString();
        var projectionsManager = GrainFactory.GetGrain<IProjectionsManager>(eventStore);
        var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
        var targeting = projectionDefinitions.Where(_ => _.ReadModel == readModel).Select(_ => _.Identifier).ToArray();
        if (targeting.Length == 0)
        {
            return;
        }

        var namespaces = GrainFactory.GetGrain<INamespaces>(eventStore);
        var allNamespaces = await namespaces.GetAll();
        foreach (var @namespace in allNamespaces)
        {
            foreach (var projectionId in targeting)
            {
                projectionPipelines.EvictFor(eventStore, @namespace, projectionId);
            }
        }
    }
}
