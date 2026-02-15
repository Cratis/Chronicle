// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionsService"><see cref="IProjectionsServiceClient"/> for managing projections.</param>
/// <param name="languageService"><see cref="Generator"/> for generating projection declaration language strings.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public class ProjectionsManager(
    IProjectionFactory projectionFactory,
    IProjectionsServiceClient projectionsService,
    ILanguageService languageService,
    IStorage storage,
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

        // Merge new definitions with existing ones, replacing any with the same identifier
        var existingProjections = State.Projections.ToList();
        foreach (var newDefinition in definitions)
        {
            var existingIndex = existingProjections.FindIndex(p => p.Identifier == newDefinition.Identifier);
            if (existingIndex >= 0)
            {
                existingProjections[existingIndex] = newDefinition;
            }
            else
            {
                existingProjections.Add(newDefinition);
            }
        }

        State.Projections = existingProjections;
        await WriteStateAsync();
        await SetDefinitionAndSubscribeForAllProjections();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions() => Task.FromResult(State.Projections);

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionWithDeclaration>> GetProjectionDeclarations()
    {
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();
        return State.Projections
            .Select(definition =>
            {
                var readModel = readModelDefinitions.SingleOrDefault(rm => rm.Identifier == definition.ReadModel);
                if (readModel is null)
                {
                    logger.MissingReadModelDefinitionForProjection(definition.Identifier, definition.ReadModel);
                    return null;
                }

                return new ProjectionWithDeclaration(
                    definition.Identifier,
                    readModel.ContainerName,
                    languageService.Generate(definition, readModel));
            })
            .Where(_ => _ is not null)
            .Select(_ => _!)
            .ToArray();
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
            var readModelDefinition = readModelDefinitions.SingleOrDefault(rm => rm.Identifier == projectionDefinition.ReadModel);
            if (readModelDefinition is null)
            {
                logger.MissingReadModelDefinitionForProjection(projectionDefinition.Identifier, projectionDefinition.ReadModel);
                continue;
            }

            await SubscribeIfNotSubscribed(projectionDefinition, readModelDefinition, added.Namespace);
        }
    }

    async Task SetDefinitionAndSubscribeForAllProjections()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();

        foreach (var definition in State.Projections)
        {
            var readModelDefinition = readModelDefinitions.SingleOrDefault(rm => rm.Identifier == definition.ReadModel);
            if (readModelDefinition is null)
            {
                logger.MissingReadModelDefinitionForProjection(definition.Identifier, definition.ReadModel);
                continue;
            }

            await SetDefinitionAndSubscribeForProjection(namespaces, definition, readModelDefinition);
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
            var eventStoreStorage = storage.GetEventStore(_eventStoreName);
            var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
            var projection = await projectionFactory.Create(_eventStoreName, namespaceName, definition, readModelDefinition, eventTypeSchemas);

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
