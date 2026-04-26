// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans;
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
        var tasks = State.Subscriptions.Select(definition => EnsureSubscriptionOnActivation(definition, namespaces));
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
        var definition = State.Subscriptions.FirstOrDefault(s => s.Identifier == subscriptionId);
        if (definition is null)
        {
            return;
        }

        var subscriptions = State.Subscriptions.ToList();
        subscriptions.RemoveAll(s => s.Identifier == subscriptionId);
        State.Subscriptions = subscriptions;
        await WriteStateAsync();

        await RemoveReminder(GetSubscriptionReminderName(subscriptionId));

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        await Unsubscribe(namespaces, definition);
    }

    /// <inheritdoc/>
    public async Task WaitUntilSubscribed(EventStoreSubscriptionId subscriptionId, TimeSpan timeout)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        var namespacesToWaitFor = namespaces.ToList();

        var startTime = DateTime.UtcNow;

        // The subscription definition is written by the EventStoreSubscriptionsReactor reacting to the
        // EventStoreSubscriptionAdded event. That reactor processes asynchronously, so the definition may
        // not yet be in State.Subscriptions when this method is called immediately after appending the event.
        // Poll until the definition appears in state before proceeding to check IsSubscribed.
        EventStoreSubscriptionDefinition? definition = null;
        while (DateTime.UtcNow - startTime < timeout)
        {
            definition = State.Subscriptions.FirstOrDefault(s => s.Identifier == subscriptionId);
            if (definition is not null)
            {
                break;
            }

            await Task.Delay(10);
        }

        if (definition is null)
        {
            logger.SubscriptionDefinitionNotFoundWithinTimeout(subscriptionId, timeout);
            throw new TimeoutException($"Subscription '{subscriptionId}' was not registered within {timeout.TotalMilliseconds}ms");
        }

        while (DateTime.UtcNow - startTime < timeout)
        {
            var tasks = namespacesToWaitFor.Select(ns => CheckSubscriptionForNamespace(definition, ns));
            var results = await Task.WhenAll(tasks);

            if (results.All(r => r))
            {
                logger.SubscriptionReadyForUse(subscriptionId);
                return;
            }

            // Brief delay before retrying
            await Task.Delay(10);
        }

        logger.SubscriptionNotReadyWithinTimeout(subscriptionId, timeout);
        throw new TimeoutException($"Subscription '{subscriptionId}' did not become ready within {timeout.TotalMilliseconds}ms");
    }

    /// <inheritdoc/>
    public async Task SourceEventStoreAdded(EventStoreName sourceEventStore)
    {
        var pendingDefinitions = State.Subscriptions
            .Where(_ => _.SourceEventStore == sourceEventStore)
            .ToArray();

        if (pendingDefinitions.Length == 0)
        {
            return;
        }

        logger.SourceEventStoreBecameAvailable(sourceEventStore, pendingDefinitions.Length);

        var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
        foreach (var definition in pendingDefinitions)
        {
            try
            {
                await RefreshSubscription(definition, namespaces);
            }
            catch (OrleansException ex)
            {
                logger.ErrorRefreshingForNewSourceEventStore(ex, definition.Identifier, sourceEventStore);
            }
            catch (TimeoutException ex)
            {
                logger.ErrorRefreshingForNewSourceEventStore(ex, definition.Identifier, sourceEventStore);
            }
            catch (OperationCanceledException ex)
            {
                logger.ErrorRefreshingForNewSourceEventStore(ex, definition.Identifier, sourceEventStore);
            }
        }
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

        try
        {
            var namespaces = await GrainFactory.GetGrain<INamespaces>(_targetEventStoreName).GetAll();
            var healthCheckTasks = namespaces.Select(ns => HealthCheckSubscription(definition, ns));
            var healthCheckResults = await Task.WhenAll(healthCheckTasks);

            // If any namespace is not healthy, refresh the subscription
            if (healthCheckResults.Any(r => !r))
            {
                logger.SubscriptionHealthCheckFailed(definition.Identifier);
                await RefreshSubscription(definition, namespaces);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.ErrorProcessingSubscriptionReminder(ex, definition.Identifier);
        }
        finally
        {
            // Re-schedule the reminder for the next check
            await ScheduleSubscriptionReminder(definition.Identifier);
        }
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

    async Task<bool> HealthCheckSubscription(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
    {
        try
        {
            var observer = GetObserver(definition, namespaceName);
            return await observer.IsSubscribed();
        }
        catch
        {
            return false;
        }
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        var tasks = State.Subscriptions.ToList().Select(definition => SubscribeIfNotSubscribed(definition, added.Namespace));
        await Task.WhenAll(tasks);
    }

    async Task EnsureSubscriptionOnActivation(EventStoreSubscriptionDefinition definition, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        await ScheduleSubscriptionReminder(definition.Identifier);
        await RefreshSubscription(definition, namespaces);
    }

    async Task RefreshSubscription(EventStoreSubscriptionDefinition definition, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        var tasks = namespaces.Select(namespaceName => RefreshSubscription(definition, namespaceName));
        await Task.WhenAll(tasks);
    }

    async Task RefreshSubscription(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
    {
        var observer = GetObserver(definition, namespaceName);

        try
        {
            var subscribed = await observer.IsSubscribed();
            if (subscribed)
            {
                // Already subscribed - no need to unsubscribe and re-subscribe
                // This eliminates the event loss gap that occurs every 60 seconds
                logger.SubscriptionAlreadyActive(definition.Identifier, namespaceName);
                return;
            }

            // Not subscribed yet, subscribe now
            logger.Subscribing(definition.Identifier, namespaceName);
            await observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
                ObserverType.External,
                definition.EventTypes.ToArray(),
                localSiloDetails.SiloAddress,
                _targetEventStoreName.Value);
        }
        catch (Exception ex)
        {
            logger.ErrorRefreshingSubscription(ex, definition.Identifier, namespaceName);
            throw;
        }
    }

    async Task<bool> CheckSubscriptionForNamespace(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
    {
        try
        {
            var observer = GetObserver(definition, namespaceName);
            return await observer.IsSubscribed();
        }
        catch
        {
            return false;
        }
    }

    async Task SubscribeIfNotSubscribed(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName namespaceName)
    {
        var observer = GetObserver(definition, namespaceName);

        logger.Subscribing(definition.Identifier, namespaceName);
        await observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            definition.EventTypes.ToArray(),
            localSiloDetails.SiloAddress,
            _targetEventStoreName.Value);
    }

    async Task Unsubscribe(IEnumerable<EventStoreNamespaceName> namespaces, EventStoreSubscriptionDefinition definition)
    {
        logger.Unregistering(definition.Identifier);

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
