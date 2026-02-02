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
        await SetDefinitionAndSubscribeInAllNamespaces(State.Webhooks);
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<WebhookDefinition> definitions)
    {
        await SetDefinitionAndSubscribeInAllNamespaces(definitions);
    }

    /// <inheritdoc/>
    public async Task Unregister(IEnumerable<WebhookId> webhookIds)
    {
        await UnsubscribeInAllNamespaces(webhookIds);
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
        var tasks = State.Webhooks.ToList().Select(definition => SubscribeIfNotSubscribed(definition, added.Namespace));
        await Task.WhenAll(tasks);
    }

    async Task SetDefinitionAndSubscribeInAllNamespaces(IEnumerable<WebhookDefinition> definitions)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();

        var tasks = definitions.Select(definition => SetDefinitionAndSubscribeForWebhook(namespaces, definition));
        await Task.WhenAll(tasks);
    }

    async Task UnsubscribeInAllNamespaces(IEnumerable<WebhookId> webhookIds)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();

        var tasks = webhookIds.Select(webhookId => UnsubscribeWebhook(namespaces, webhookId));
        await Task.WhenAll(tasks);
    }

    async Task SetDefinitionAndSubscribeForWebhook(IEnumerable<EventStoreNamespaceName> namespaces, WebhookDefinition definition)
    {
        logger.SettingDefinition(definition.Identifier);
        var key = new WebhookKey(definition.Identifier, _eventStoreName);
        var webhook = GrainFactory.GetGrain<IWebhook>(key);
        await webhook.SetDefinition(definition);

        if (!definition.IsActive)
        {
            logger.NotActive(definition.Identifier);
            return;
        }

        var tasks = namespaces.Select(namespaceName => SubscribeIfNotSubscribed(definition, namespaceName));
        await Task.WhenAll(tasks);
    }

    async Task UnsubscribeWebhook(IEnumerable<EventStoreNamespaceName> namespaces, WebhookId webhookId)
    {
        logger.Unregistering(webhookId);
        var key = new WebhookKey(webhookId, _eventStoreName);
        var webhook = GrainFactory.GetGrain<IWebhook>(key);
        var definition = await webhook.GetDefinition();

        var tasks = namespaces.Select(namespaceName => UnsubscribeIfSubscribed(definition, namespaceName));
        await Task.WhenAll(tasks);
    }

    async Task SubscribeIfNotSubscribed(WebhookDefinition definition,  EventStoreNamespaceName namespaceName)
    {
        var observer = GetObserver(definition, namespaceName);
        var subscribed = await observer.IsSubscribed();

        if (!subscribed && definition.IsActive)
        {
            logger.Subscribing(definition.Identifier, namespaceName);
            await observer.Subscribe<IWebhookObserverSubscriber>(
                ObserverType.External,
                definition.EventTypes.ToArray(),
                localSiloDetails.SiloAddress);
            return;
        }
        logger.AlreadySubscribed(definition.Identifier, namespaceName);
    }

    async Task UnsubscribeIfSubscribed(WebhookDefinition definition,  EventStoreNamespaceName namespaceName)
    {
        var observer = GetObserver(definition, namespaceName);
        var subscribed = await observer.IsSubscribed();

        if (subscribed)
        {
            logger.Unsubscribing(definition.Identifier, namespaceName);
            await observer.Unsubscribe();
            return;
        }
        logger.AlreadyUnsubscribed(definition.Identifier, namespaceName);
    }

    IObserver GetObserver(WebhookDefinition definition, EventStoreNamespaceName namespaceName) =>
        GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));
}
