// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grains.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooksManager"/>.
/// </summary>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.WebhooksManager)]
public class WebhooksManager(
    ILocalSiloDetails localSiloDetails,
    ILogger<WebhooksManager> logger) : Grain<WebhooksManagerState>, IWebhooksManager, IOnBroadcastChannelSubscribed
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
        State.Webhooks = definitions;
        await WriteStateAsync();
        await SetDefinitionAndSubscribeForAllWebhooks();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<WebhookDefinition>> GetWebhookDefinitions() => Task.FromResult(State.Webhooks);

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        var eventStore = streamSubscription.ChannelId.GetKeyAsString();
        if (_eventStoreName != eventStore) return Task.CompletedTask;

        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;

        static Task OnError(Exception exception) => Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        foreach (var definitions in State.Webhooks)
        {
            var key = new WebhookKey(definitions.Identifier, _eventStoreName);
            var webhook = GrainFactory.GetGrain<IWebhook>(key);
            await webhook.SetDefinition(definitions);
            await SubscribeIfNotSubscribed(definitions, added.Namespace);
        }
    }

    async Task SetDefinitionAndSubscribeForAllWebhooks()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();

        foreach (var definition in State.Webhooks)
        {
            await SetDefinitionAndSubscribeForWebhook(namespaces, definition);
        }
    }

    async Task SetDefinitionAndSubscribeForWebhook(IEnumerable<EventStoreNamespaceName> namespaces, WebhookDefinition definition)
    {
        logger.SettingDefinition(definition.Identifier);
        var key = new WebhookKey(definition.Identifier, _eventStoreName);
        var webhook = GrainFactory.GetGrain<IWebhook>(key);
        await webhook.SetDefinition(definition);

        if (!definition.IsActive)
        {
            return;
        }

        foreach (var namespaceName in namespaces)
        {
            await SubscribeIfNotSubscribed(definition, namespaceName);
        }
    }

    async Task SubscribeIfNotSubscribed(WebhookDefinition definition,  EventStoreNamespaceName namespaceName)
    {
        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));
        var subscribed = await observer.IsSubscribed();

        if (!subscribed && definition.IsActive)
        {
            logger.Subscribing(definition.Identifier, namespaceName);
            await observer.Subscribe<IWebhookObserverSubscriber>(
                ObserverType.Webhook,
                definition.EventTypes.ToArray(),
                localSiloDetails.SiloAddress);
        }
    }
}
