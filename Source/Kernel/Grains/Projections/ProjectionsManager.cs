// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.LanguageDefinition;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionsService"><see cref="IProjectionsServiceClient"/> for managing projections.</param>
/// <param name="projectionDslGenerator"><see cref="Generator"/> for generating projection DSL strings.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public class ProjectionsManager(
    IProjectionFactory projectionFactory,
    IProjectionsServiceClient projectionsService,
    Generator projectionDslGenerator,
    ILocalSiloDetails localSiloDetails,
    ILogger<ProjectionsManager> logger) : Grain<ProjectionsManagerState>, IProjectionsManager, IOnBroadcastChannelSubscribed
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        await SetDefinitionAndSubscribeForAllProjections();
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionDefinition> definitions)
    {
        await projectionsService.Register(_eventStoreName, definitions);
        State.Projections = definitions;
        await SetDefinitionAndSubscribeForAllProjections();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions()
    {
        return Task.FromResult(State.Projections);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionWithDsl>> GetProjectionDsls()
    {
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();
        return State.Projections
            .Select(definition =>
            {
                var readModel = readModelDefinitions.Single(rm => rm.Identifier == definition.ReadModel);
                var readModelSchema = readModel.GetSchemaForLatestGeneration();
                return new ProjectionWithDsl(
                definition.Identifier,
                readModelSchema.Title ?? readModel.Identifier,
                projectionDslGenerator.Generate(definition, readModel));
            }).ToArray();
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        var eventStore = streamSubscription.ChannelId.GetKeyAsString();
        if (_eventStoreName != eventStore) return Task.CompletedTask;

        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        await projectionsService.NamespaceAdded(_eventStoreName, added.Namespace);
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();

        foreach (var projectionDefinition in State.Projections)
        {
            var key = new ProjectionKey(projectionDefinition.Identifier, _eventStoreName);
            var projection = GrainFactory.GetGrain<IProjection>(key);
            await projection.SetDefinition(projectionDefinition);
            await SubscribeIfNotSubscribed(projectionDefinition, readModelDefinitions.Single(rm => rm.Identifier == projectionDefinition.ReadModel), added.Namespace);
        }
    }

    async Task SetDefinitionAndSubscribeForAllProjections()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();

        foreach (var definition in State.Projections)
        {
            await SetDefinitionAndSubscribeForProjection(namespaces, definition, readModelDefinitions.Single(rm => rm.Identifier == definition.ReadModel));
        }
    }

    async Task SetDefinitionAndSubscribeForProjection(IEnumerable<EventStoreNamespaceName> namespaces, ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        logger.SettingDefinition(definition.Identifier);
        var key = new ProjectionKey(definition.Identifier, _eventStoreName);
        var projection = GrainFactory.GetGrain<IProjection>(key);
        await projection.SetDefinition(definition);

        if (!definition.IsActive)
        {
            return;
        }

        foreach (var namespaceName in namespaces)
        {
            await SubscribeIfNotSubscribed(definition, readModelDefinition, namespaceName);
        }
    }

    async Task SubscribeIfNotSubscribed(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, EventStoreNamespaceName namespaceName)
    {
        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));
        var subscribed = await observer.IsSubscribed();

        if (!subscribed && definition.IsActive)
        {
            logger.Subscribing(definition.Identifier, namespaceName);
            var projection = await projectionFactory.Create(_eventStoreName, namespaceName, definition, readModelDefinition);

            logger.SubscribingWithEventTypes(
                definition.Identifier,
                projection.EventTypes.Count(),
                string.Join(", ", projection.EventTypes.Select(et => et.Id)));

            await observer.Subscribe<IProjectionObserverSubscriber>(
                ObserverType.Projection,
                projection.EventTypes,
                localSiloDetails.SiloAddress);
        }
    }

    Task OnError(Exception exception) => Task.CompletedTask;
}
