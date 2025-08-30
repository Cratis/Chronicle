// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsService"/>.
/// </summary>
/// <param name="grainId"><see cref="GrainId"/> for the grain.</param>
/// <param name="silo"><see cref="Silo"/> the grain belongs to.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="projections"><see cref="Chronicle.Projections.IProjectionsManager"/> for managing projections.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[ImplicitChannelSubscription]
public class ProjectionsService(
    GrainId grainId,
    Silo silo,
    IGrainFactory grainFactory,
    IStorage storage,
    Chronicle.Projections.IProjectionsManager projections,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IProjectionsService
{
    /// <inheritdoc/>
    public override async Task Init(IServiceProvider serviceProvider)
    {
        var eventStores = await storage.Cluster.GetEventStores();
        foreach (var eventStore in eventStores)
        {
            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            var definitions = await projectionsManager.GetProjectionDefinitions();
            await Register(eventStore, definitions);
        }
    }

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
    {
        var readModelDefinitions = await grainFactory.GetReadModelsManager(eventStore).GetDefinitions();
        var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
        var allNamespaces = await namespaces.GetAll();
        await projections.Register(eventStore, definitions, readModelDefinitions, allNamespaces);
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var readModelDefinitions = await grainFactory.GetGrain<IReadModelsManager>(eventStore).GetDefinitions();
        await projections.AddNamespace(eventStore, @namespace, readModelDefinitions);
    }
}
