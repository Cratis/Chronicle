// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
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
    ILogger<WebhooksManager> logger) : Grain<WebhooksManagerState>, IWebhooksManager, IOnBroadcastChannelSubscribed, IRemindable
{
    const string SubscriptionReminderPrefix = "webhook-subscribe:";
    static readonly TimeSpan _subscriptionReminderDelay = TimeSpan.FromMilliseconds(100);
    static readonly TimeSpan _subscriptionReminderPeriod = TimeSpan.FromMinutes(1);

    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var tasks = State.Webhooks.Select(definition => SetDefinitionAndSubscribe(namespaces, definition));
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task Add(WebhookDefinition definition)
    {
        var webhooks = State.Webhooks.ToList();
        webhooks.Add(definition);
        State.Webhooks = webhooks;
        await WriteStateAsync();

        await ScheduleSubscriptionReminder(definition.Identifier);
    }

    /// <inheritdoc/>
    public async Task Update(WebhookDefinition definition)
    {
        var webhooks = State.Webhooks.ToList();
        var index = webhooks.FindIndex(w => w.Identifier == definition.Identifier);
        if (index >= 0)
        {
            webhooks[index] = definition;
            State.Webhooks = webhooks;
            await WriteStateAsync();

            await ScheduleSubscriptionReminder(definition.Identifier);
        }
    }

    /// <inheritdoc/>
    public async Task Remove(WebhookId webhookId)
    {
        var webhooks = State.Webhooks.ToList();
        webhooks.RemoveAll(w => w.Identifier == webhookId);
        State.Webhooks = webhooks;
        await WriteStateAsync();

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        await Unsubscribe(namespaces, webhookId);
    }

    /// <inheritdoc/>
    public async Task SetAuthorization(WebhookId webhookId, WebhookAuthorization authorization)
    {
        var webhooks = State.Webhooks.ToList();
        var index = webhooks.FindIndex(w => w.Identifier == webhookId);
        if (index >= 0)
        {
            var definition = webhooks[index];
            var updatedTarget = new WebhookTarget(definition.Target.Url, authorization, definition.Target.Headers);
            webhooks[index] = definition with { Target = updatedTarget };
            State.Webhooks = webhooks;
            await WriteStateAsync();

            var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
            await SetDefinitionAndSubscribe(namespaces, webhooks[index]);
        }
    }

    /// <inheritdoc/>
    public async Task SetEventTypes(WebhookId webhookId, IEnumerable<EventType> eventTypes)
    {
        var webhooks = State.Webhooks.ToList();
        var index = webhooks.FindIndex(w => w.Identifier == webhookId);
        if (index >= 0)
        {
            webhooks[index] = webhooks[index] with { EventTypes = eventTypes };
            State.Webhooks = webhooks;
            await WriteStateAsync();

            var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
            await SetDefinitionAndSubscribe(namespaces, webhooks[index]);
        }
    }

    /// <inheritdoc/>
    public async Task SetTargetUrl(WebhookId webhookId, WebhookTargetUrl targetUrl)
    {
        var webhooks = State.Webhooks.ToList();
        var index = webhooks.FindIndex(w => w.Identifier == webhookId);
        if (index >= 0)
        {
            var definition = webhooks[index];
            var updatedTarget = new WebhookTarget(targetUrl, definition.Target.Authorization, definition.Target.Headers);
            webhooks[index] = definition with { Target = updatedTarget };
            State.Webhooks = webhooks;
            await WriteStateAsync();

            var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
            await SetDefinitionAndSubscribe(namespaces, webhooks[index]);
        }
    }

    /// <inheritdoc/>
    public async Task SetTargetHeaders(WebhookId webhookId, IReadOnlyDictionary<string, string> headers)
    {
        var webhooks = State.Webhooks.ToList();
        var index = webhooks.FindIndex(w => w.Identifier == webhookId);
        if (index >= 0)
        {
            var definition = webhooks[index];
            var updatedTarget = new WebhookTarget(definition.Target.Url, definition.Target.Authorization, headers);
            webhooks[index] = definition with { Target = updatedTarget };
            State.Webhooks = webhooks;
            await WriteStateAsync();

            var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
            await SetDefinitionAndSubscribe(namespaces, webhooks[index]);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<WebhookDefinition>> GetWebhookDefinitions() => Task.FromResult(State.Webhooks);

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (!TryGetWebhookIdFromReminder(reminderName, out var webhookId))
        {
            return;
        }

        var definition = State.Webhooks.FirstOrDefault(_ => _.Identifier == webhookId);
        if (definition is null)
        {
            await RemoveReminder(reminderName);
            return;
        }

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        await SetDefinitionAndSubscribe(namespaces, definition);
        await RemoveReminder(reminderName);
    }

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

    async Task SetDefinitionAndSubscribe(IEnumerable<EventStoreNamespaceName> namespaces, WebhookDefinition definition)
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

    async Task Unsubscribe(IEnumerable<EventStoreNamespaceName> namespaces, WebhookId webhookId)
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

    Task<IGrainReminder> ScheduleSubscriptionReminder(WebhookId webhookId)
    {
        var reminderName = GetSubscriptionReminderName(webhookId);
        return this.RegisterOrUpdateReminder(reminderName, _subscriptionReminderDelay, _subscriptionReminderPeriod);
    }

    async Task RemoveReminder(string reminderName)
    {
        var reminder = await this.GetReminder(reminderName);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }
    }

    bool TryGetWebhookIdFromReminder(string reminderName, out WebhookId webhookId)
    {
        if (!reminderName.StartsWith(SubscriptionReminderPrefix, StringComparison.Ordinal))
        {
            webhookId = WebhookId.Unspecified;
            return false;
        }

        var id = reminderName[SubscriptionReminderPrefix.Length..];
        if (string.IsNullOrWhiteSpace(id))
        {
            webhookId = WebhookId.Unspecified;
            return false;
        }

        webhookId = new WebhookId(id);
        return true;
    }

    string GetSubscriptionReminderName(WebhookId webhookId) => $"{SubscriptionReminderPrefix}{webhookId.Value}";
}
