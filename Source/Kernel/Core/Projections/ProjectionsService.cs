// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using IEngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsService"/>.
/// </summary>
/// <param name="grainId"><see cref="GrainId"/> for the grain.</param>
/// <param name="silo"><see cref="Silo"/> the grain belongs to.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="projections"><see cref="Engine.IProjectionsManager"/> for managing projections.</param>
/// <param name="projectionPipelines"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[ImplicitChannelSubscription]
public class ProjectionsService(
    GrainId grainId,
    Silo silo,
    IGrainFactory grainFactory,
    IEngineProjectionsManager projections,
    IProjectionPipelineManager projectionPipelines,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IProjectionsService
{
    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
    {
        var readModelDefinitions = await grainFactory.GetReadModelsManager(eventStore).GetDefinitions();
        var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
        var allNamespaces = await namespaces.GetAll();
        EvictProjections(eventStore, definitions, allNamespaces);

        await projections.Register(eventStore, definitions, readModelDefinitions, allNamespaces);
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var readModelDefinitions = await grainFactory.GetGrain<IReadModelsManager>(eventStore).GetDefinitions();
        await projections.AddNamespace(eventStore, @namespace, readModelDefinitions);
    }

    void EvictProjections(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<EventStoreNamespaceName> allNamespaces)
    {
        foreach (var definition in definitions)
        {
            projections.Evict(eventStore, definition.Identifier);

            foreach (var @namespace in allNamespaces)
            {
                projectionPipelines.EvictFor(eventStore, @namespace, definition.Identifier);
            }
        }
    }
}
