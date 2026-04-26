// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles event store subscription lifecycle events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for getting all event stores.</param>
/// <param name="logger">The <see cref="ILogger{EventStoreSubscriptionsReactor}"/> for logging.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: false, defaultNamespaceOnly: true)]
public class EventStoreSubscriptionsReactor(IGrainFactory grainFactory, IStorage storage, ILogger<EventStoreSubscriptionsReactor> logger) : Reactor
{
    /// <summary>
    /// Handles the addition of an event store subscription.
    /// </summary>
    /// <param name="event">The event containing the subscription information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(EventStoreSubscriptionAdded @event, EventContext eventContext)
    {
        logger.AddingSubscription(eventContext.EventSourceId);

        var subscriptionId = new EventStoreSubscriptionId(eventContext.EventSourceId.Value);
        var definition = new EventStoreSubscriptionDefinition(
            subscriptionId,
            @event.SourceEventStore,
            @event.EventTypes);

        var subscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventContext.EventStore.Value);
        await subscriptionsManager.Add(definition);

        logger.SubscriptionAdded(eventContext.EventSourceId, subscriptionId);
    }

    /// <summary>
    /// Handles the removal of an event store subscription.
    /// </summary>
    /// <param name="event">The event containing the subscription information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(EventStoreSubscriptionRemoved @event, EventContext eventContext)
    {
        logger.RemovingSubscription(eventContext.EventSourceId);

        var subscriptionId = new EventStoreSubscriptionId(eventContext.EventSourceId.Value);
        var subscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventContext.EventStore.Value);
        await subscriptionsManager.Remove(subscriptionId);

        logger.SubscriptionRemoved(eventContext.EventSourceId);
    }

    /// <summary>
    /// Handles the addition of a source event store and retries any pending subscriptions immediately.
    /// </summary>
    /// <param name="event">The event containing the event store information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task EventStoreAdded(EventStoreAdded @event, EventContext eventContext)
    {
        if (eventContext.EventStore != Concepts.EventStoreName.System)
        {
            return;
        }

        var targetEventStores = (await storage.GetEventStores()).ToArray();
        logger.SourceEventStoreAdded(@event.EventStore, targetEventStores.Length);

        var tasks = targetEventStores.Select(targetEventStore =>
            NotifySubscriptionsManagerOfSourceEventStore(targetEventStore, @event.EventStore));
        await Task.WhenAll(tasks);
    }

    async Task NotifySubscriptionsManagerOfSourceEventStore(Concepts.EventStoreName targetEventStore, Concepts.EventStoreName sourceEventStore)
    {
        try
        {
            var subscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(targetEventStore.Value);
            await subscriptionsManager.SourceEventStoreAdded(sourceEventStore);
        }
        catch (Exception ex)
        {
            logger.ErrorRetryingPendingSubscriptions(ex, targetEventStore, sourceEventStore);
        }
    }
}
