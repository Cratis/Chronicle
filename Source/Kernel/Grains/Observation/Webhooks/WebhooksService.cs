// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooksService"/>.
/// </summary>
/// <param name="grainId"><see cref="GrainId"/> for the grain.</param>
/// <param name="silo"><see cref="Silo"/> the grain belongs to.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="webhooks"><see cref="IWebhooksManager"/> for managing projections.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[ImplicitChannelSubscription]
public class WebhooksService(
    GrainId grainId,
    Silo silo,
    IGrainFactory grainFactory,
    IStorage storage,
    IWebhooksManager webhooks,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IWebhooksService
{
    /// <inheritdoc/>
    public override async Task Init(IServiceProvider serviceProvider)
    {
        var eventStores = await storage.GetEventStores();
        foreach (var eventStore in eventStores)
        {
            var projectionsManager = grainFactory.GetGrain<IWebhooksManager>(eventStore);
            var definitions = await projectionsManager.GetProjectionDefinitions();
            await Register(eventStore, definitions);
        }
    }

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<WebhookDefinition> definitions)
    {
        var readModelDefinitions = await grainFactory.GetReadModelsManager(eventStore).GetDefinitions();
        var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
        var allNamespaces = await namespaces.GetAll();
        await webhooks.Register(eventStore, definitions, readModelDefinitions, allNamespaces);
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var readModelDefinitions = await grainFactory.GetGrain<IReadModelsManager>(eventStore).GetDefinitions();
        await webhooks.AddNamespace(eventStore, @namespace, readModelDefinitions);
    }
}
