// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ConceptsEventStoreSubscriptionDefinition = Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;
using ContractEventStoreSubscriptionDefinition = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;
using ContractIEventStoreSubscriptions = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions;

namespace Cratis.Chronicle.Services.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="ContractIEventStoreSubscriptions"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing subscription definitions.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <param name="logger">The logger.</param>
internal sealed class EventStoreSubscriptions(
    IGrainFactory grainFactory,
    IStorage storage,
    IOptions<ChronicleOptions> options,
    ILogger<EventStoreSubscriptions> logger) : ContractIEventStoreSubscriptions
{
    /// <inheritdoc/>
    public async Task Add(AddEventStoreSubscriptions request, CallContext context = default)
    {
        // Validate every pinned registration before reserving or appending any definition in this request.
        foreach (var subscription in request.Subscriptions.Where(_ => _.GenerationDelivery == Contracts.Observation.EventGenerationDelivery.Pinned))
        {
            var pins = subscription.EventTypes.Select(et => new EventType(et.Id, et.Generation)).ToArray();
            EventStoreSubscriptionGenerationValidation.ValidatePins(pins);
            foreach (var pin in pins.Distinct())
            {
                await EventStoreSubscriptionGenerationValidation.GetExactSchema(storage.GetEventStore(subscription.SourceEventStore).EventTypes, pin);
                await EventStoreSubscriptionGenerationValidation.GetExactSchema(storage.GetEventStore(request.TargetEventStore).EventTypes, pin);
            }
        }

        var registrationDiagnosticId = Guid.NewGuid();
        var eventSequence = grainFactory.GetSystemEventSequence(request.TargetEventStore);
        var subscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(request.TargetEventStore);

        foreach (var subscription in request.Subscriptions)
        {
            var definition = new ConceptsEventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(subscription.Identifier),
                new EventStoreName(subscription.SourceEventStore),
                subscription.EventTypes.Select(et => new EventType(et.Id, et.Generation)).ToArray())
            {
                GenerationDelivery = (EventGenerationDelivery)subscription.GenerationDelivery
            };

            var existingSubscriptions = await subscriptionsManager.GetSubscriptionDefinitions();
            var existing = existingSubscriptions.FirstOrDefault(s => s.Identifier == definition.Identifier);

            var hasMatchingDefinition = existing is not null && HasSameDefinition(existing, definition);
            logger.RegistrationDecision(request.TargetEventStore, definition.SourceEventStore, definition.Identifier, registrationDiagnosticId, existingSubscriptions.TryGetNonEnumeratedCount(out var memoryCount) ? memoryCount : -1, hasMatchingDefinition);

            if (!hasMatchingDefinition)
            {
                // Reserve the definition in the manager before appending the event so a second
                // immediate Subscribe() call observes the pending subscription instead of
                // appending a duplicate EventStoreSubscriptionAdded event.
                logger.ManagerAddStarting(request.TargetEventStore, definition.Identifier, registrationDiagnosticId);
                await subscriptionsManager.Add(definition);
                logger.ManagerAddCompleted(request.TargetEventStore, definition.Identifier, registrationDiagnosticId);
                logger.SubscriptionAppendStarting(request.TargetEventStore, definition.Identifier, registrationDiagnosticId);
                await eventSequence.Append(subscription.Identifier, new EventStoreSubscriptionAdded(
                    definition.SourceEventStore,
                    definition.EventTypes)
                {
                    GenerationDelivery = definition.GenerationDelivery
                });
                logger.SubscriptionAppendCompleted(request.TargetEventStore, definition.Identifier, registrationDiagnosticId);
            }

            // Existing matching definitions still need to be reactivated after reconnects and server restarts.
            // Without this, subscriptions can remain persisted but disconnected indefinitely.
            await subscriptionsManager.SourceEventStoreAdded(definition.SourceEventStore);

            // Wait for the subscription to be ready before returning to the client.
            // This ensures that events are not lost if the client immediately starts publishing.
            try
            {
                await subscriptionsManager.WaitUntilSubscribed(
                    new EventStoreSubscriptionId(subscription.Identifier),
                    TimeSpan.FromSeconds(options.Value.Observers.SubscriptionReadyTimeout));
            }
            catch (TimeoutException) when (definition.GenerationDelivery == EventGenerationDelivery.Compatibility)
            {
                // Log but don't fail - the subscription may still activate asynchronously.
                // This prevents blocking the client indefinitely if there are issues.
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The removals are appended together rather than one at a time, so the order in which they land in the system
    /// event sequence relative to each other is no longer the order they appear in the request. Each removal targets
    /// its own event source - the subscription identifier - so per-stream ordering is unaffected, and observers
    /// partition by event source, so nothing observes the relative order of two different subscriptions being removed.
    /// </remarks>
    public async Task Remove(RemoveEventStoreSubscriptions request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetSystemEventSequence(request.TargetEventStore);
        await Task.WhenAll(request.SubscriptionIds.Select(subscriptionId => eventSequence.Append(subscriptionId, new EventStoreSubscriptionRemoved())));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ContractEventStoreSubscriptionDefinition>> GetSubscriptions(GetEventStoreSubscriptionsRequest request)
    {
        var definitions = await storage.GetEventStore(request.TargetEventStore).EventStoreSubscriptions.GetAll();
        logger.PersistedDefinitionsRead(request.TargetEventStore, definitions.TryGetNonEnumeratedCount(out var definitionCount) ? definitionCount : -1);
        return definitions.Select(definition => new ContractEventStoreSubscriptionDefinition
        {
            Identifier = definition.Identifier.Value,
            SourceEventStore = definition.SourceEventStore.Value,
            EventTypes = definition.EventTypes.Select(et => new Contracts.Events.EventType { Id = et.Id, Generation = et.Generation }).ToList(),
            GenerationDelivery = (Contracts.Observation.EventGenerationDelivery)definition.GenerationDelivery
        });
    }

    static bool HasSameDefinition(ConceptsEventStoreSubscriptionDefinition existing, ConceptsEventStoreSubscriptionDefinition incoming)
    {
        if (existing.SourceEventStore != incoming.SourceEventStore || existing.GenerationDelivery != incoming.GenerationDelivery)
        {
            return false;
        }

        var existingEventTypes = existing.EventTypes.ToHashSet();
        var incomingEventTypes = incoming.EventTypes.ToHashSet();
        return existingEventTypes.SetEquals(incomingEventTypes);
    }
}
