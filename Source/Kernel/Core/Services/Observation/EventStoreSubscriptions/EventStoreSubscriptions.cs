// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage;
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
internal sealed class EventStoreSubscriptions(
    IGrainFactory grainFactory,
    IStorage storage) : ContractIEventStoreSubscriptions
{
    /// <inheritdoc/>
    public async Task Add(AddEventStoreSubscriptions request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetSystemEventSequence(request.TargetEventStore);

        foreach (var subscription in request.Subscriptions)
        {
            var definition = new ConceptsEventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(subscription.Identifier),
                new EventStoreName(subscription.SourceEventStore),
                subscription.EventTypes.Select(et => new EventType(et.Id, et.Generation)));

            var subscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(request.TargetEventStore);
            var existingSubscriptions = await subscriptionsManager.GetSubscriptionDefinitions();
            var existing = existingSubscriptions.FirstOrDefault(s => s.Identifier == definition.Identifier);

            if (existing is null || !HasSameDefinition(existing, definition))
            {
                await eventSequence.Append(subscription.Identifier, new EventStoreSubscriptionAdded(
                    definition.SourceEventStore,
                    definition.EventTypes));
            }
        }
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveEventStoreSubscriptions request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetSystemEventSequence(request.TargetEventStore);

        foreach (var subscriptionId in request.SubscriptionIds)
        {
            await eventSequence.Append(subscriptionId, new EventStoreSubscriptionRemoved());
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ContractEventStoreSubscriptionDefinition>> GetSubscriptions(GetEventStoreSubscriptionsRequest request)
    {
        var definitions = await storage.GetEventStore(request.TargetEventStore).EventStoreSubscriptions.GetAll();
        return definitions.Select(definition => new ContractEventStoreSubscriptionDefinition
        {
            Identifier = definition.Identifier.Value,
            SourceEventStore = definition.SourceEventStore.Value,
            EventTypes = definition.EventTypes.Select(et => new Contracts.Events.EventType { Id = et.Id, Generation = et.Generation }).ToList()
        });
    }

    static bool HasSameDefinition(ConceptsEventStoreSubscriptionDefinition existing, ConceptsEventStoreSubscriptionDefinition incoming)
    {
        if (existing.SourceEventStore != incoming.SourceEventStore)
        {
            return false;
        }

        var existingEventTypes = existing.EventTypes.ToHashSet();
        var incomingEventTypes = incoming.EventTypes.ToHashSet();
        return existingEventTypes.SetEquals(incomingEventTypes);
    }
}
