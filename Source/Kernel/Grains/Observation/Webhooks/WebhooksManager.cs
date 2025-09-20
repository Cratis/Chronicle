// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;
using IProjection = Cratis.Chronicle.Projections.IProjection;
using ProjectionsManager = Cratis.Chronicle.Projections.ProjectionsManager;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="Chronicle.Projections.IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionsService"><see cref="Projections.IProjectionsServiceClient"/> for managing projections.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public class WebhooksManager(
    IProjectionFactory projectionFactory,
    IWebhooksServiceClient projectionsService,
    ILocalSiloDetails localSiloDetails,
    ILogger<ProjectionsManager> logger) : Grain<WebhooksManagerState>, IWebhooksManager
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        await SetDefinitionAndSubscribeForAllWebhooks();
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<WebhookDefinition> definitions)
    {
        await projectionsService.Register(_eventStoreName, definitions);
        State.Projections = definitions;
        await SetDefinitionAndSubscribeForAllWebhooks();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<WebhookDefinition>> GetProjectionDefinitions()
    {
        return Task.FromResult(State.Projections);
    }

    async Task SetDefinitionAndSubscribeForAllWebhooks()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();

        foreach (var definition in State.Webhooks)
        {
            await SetDefinitionAndSubscribeForWebhook(namespaces, definition);
        }
    }
    
    async Task SetDefinitionAndSubscribeForWebhook(IEnumerable<EventStoreNamespaceName> namespaces, ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        logger.SettingDefinition(definition.Identifier);
        var key = new WebhookKey(definition.Identifier, _eventStoreName);
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
    //
    // async Task SubscribeIfNotSubscribed(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, EventStoreNamespaceName namespaceName)
    // {
    //     var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));
    //     var subscribed = await observer.IsSubscribed();
    //
    //     if (!subscribed && definition.IsActive)
    //     {
    //         ObserverLogMessages.Subscribing(logger, definition.Identifier, namespaceName);
    //         var projection = await projectionFactory.Create(_eventStoreName, namespaceName, definition, readModelDefinition);
    //
    //         await observer.Subscribe<IProjectionObserverSubscriber>(
    //             ObserverType.Projection,
    //             projection.EventTypes,
    //             localSiloDetails.SiloAddress);
    //     }
    // }

    Task OnError(Exception exception) => Task.CompletedTask;
}
