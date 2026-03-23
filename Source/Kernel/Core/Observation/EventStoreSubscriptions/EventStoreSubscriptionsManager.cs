// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreSubscriptionsManager"/>.
/// </summary>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventStoreSubscriptionsManager)]
public class EventStoreSubscriptionsManager(
    ILocalSiloDetails localSiloDetails,
    ILogger<EventStoreSubscriptionsManager> logger) : Grain<EventStoreSubscriptionsState>, IEventStoreSubscriptionsManager, IOnBroadcastChannelSubscribed, IRemindable
{
    const string SubscriptionReminderPrefix = "event-store-subscription-subscribe:";
    static readonly TimeSpan _subscriptionReminderDelay = TimeSpan.FromMilliseconds(100);
    static readonly TimeSpan _subscriptionReminderPeriod = TimeSpan.FromMinutes(1);

    EventStoreName _targetEventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _targetEventStoreName = this.GetPrimaryKeyString();
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        var tasks = State.Subscriptions.Select(definition => SubscribeIfNotSubscribed(definition, namespaces));
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task Add(EventStoreSubscriptionDefinition definition)
    {
        var subscriptions = State.Subscriptions.ToList();
        subscriptions.RemoveAll(s => s.Identifier == definition.Identifier);
        subscriptions.Add(definition);
        State.Subscriptions = subscriptions;
        await WriteStateAsync();

        await ScheduleSubscriptionReminder(definition.Identifier);
    }

    /// <inheritdoc/>
    public async Task Remove(EventStoreSubscriptionId subscriptionId)
    {
        var subscriptions = State.Subscriptions.ToList();
        subscriptions.RemoveAll(s => s.Identifier == subscriptionId);
        State.Subscriptions = subscriptions;
        await WriteStateAsync();

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        await Unsubscribe(namespaces, subscriptionId);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreSubscriptionDefinition>> GetSubscriptionDefinitions() =>
        Task.FromResult(State.Subscriptions);

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (!TryGetSubscriptionIdFromReminder(reminderName, out var subscriptionId))
        {
            return;
        }

        var definition = State.Subscriptions.FirstOrDefault(_ => _.Identifier == subscriptionId);
        if (definition is null)
        {
            await RemoveReminder(reminderName);
            return;
        }

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        await SubscribeIfNotSubscribed(definition, namespaces);
        await RemoveReminder(reminderName);
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        var eventStore = streamSubscription.ChannelId.GetKeyAsString();
        if (_targetEventStoreName != eventStore) return Task.CompletedTask;

        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;

        static Task OnError(Exception exception) => Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        var tasks = State.Subscriptions.ToList().Select(definition => SubscribeIfNotSubscribed(definition, added.Namespace));
        await Task.WhenAll(tasks);
    }

    async Task SubscribeIfNotSubscribed(EventStoreSubscriptionDefinition definition, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        var tasks = namespaces.Select(namespaceName => SubscribeIfNotSubscribed(definition, namespaceName));
        await Task.WhenAll(tasks);
    }

    async Task SubscribeIfNotSubscribed(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
    {
        var observer = GetObserver(definition, namespaceName);
        var subscribed = await observer.IsSubscribed();

        if (!subscribed)
        {
            logger.Subscribing(definition.Identifier, namespaceName);
            var key = new EventStoreSubscriptionKey(definition.Identifier, _targetEventStoreName);
            await observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
                ObserverType.External,
                definition.EventTypes.ToArray(),
                localSiloDetails.SiloAddress,
                key.ToString());
            return;
        }

        logger.AlreadySubscribed(definition.Identifier, namespaceName);
    }

    async Task Unsubscribe(IEnumerable<EventStoreNamespaceName> namespaces, EventStoreSubscriptionId subscriptionId)
    {
        logger.Unregistering(subscriptionId);

        var definition = State.Subscriptions.FirstOrDefault(s => s.Identifier == subscriptionId);
        if (definition is null) return;

        var tasks = namespaces.Select(namespaceName => UnsubscribeIfSubscribed(definition, namespaceName));
        await Task.WhenAll(tasks);
    }

    async Task UnsubscribeIfSubscribed(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
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

    IObserver GetObserver(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName) =>
        GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, definition.SourceEventStore, namespaceName, EventSequenceId.Outbox));

    Task<IGrainReminder> ScheduleSubscriptionReminder(EventStoreSubscriptionId subscriptionId)
    {
        var reminderName = GetSubscriptionReminderName(subscriptionId);
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

    bool TryGetSubscriptionIdFromReminder(string reminderName, out EventStoreSubscriptionId subscriptionId)
    {
        if (!reminderName.StartsWith(SubscriptionReminderPrefix, StringComparison.Ordinal))
        {
            subscriptionId = EventStoreSubscriptionId.Unspecified;
            return false;
        }

        var id = reminderName[SubscriptionReminderPrefix.Length..];
        if (string.IsNullOrWhiteSpace(id))
        {
            subscriptionId = EventStoreSubscriptionId.Unspecified;
            return false;
        }

        subscriptionId = new EventStoreSubscriptionId(id);
        return true;
    }

    string GetSubscriptionReminderName(EventStoreSubscriptionId subscriptionId) => $"{SubscriptionReminderPrefix}{subscriptionId.Value}";
}
