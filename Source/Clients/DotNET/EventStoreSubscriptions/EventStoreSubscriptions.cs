// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Microsoft.Extensions.Logging;
using ContractEventStoreSubscriptionDefinition = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;
using ContractEventType = Cratis.Chronicle.Contracts.Events.EventType;
using IEventTypes = Cratis.Chronicle.Events.IEventTypes;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreSubscriptions"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class EventStoreSubscriptions(IEventTypes eventTypes, IEventStore eventStore, ILogger<EventStoreSubscriptions> logger) : IEventStoreSubscriptions
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Subscribe(EventStoreSubscriptionId subscriptionId, string sourceEventStore, Action<IEventStoreSubscriptionBuilder>? configure = default)
    {
        var builder = new EventStoreSubscriptionBuilder(eventTypes, subscriptionId, sourceEventStore);
        configure?.Invoke(builder);
        var definition = builder.Build();

        logger.Subscribing(subscriptionId, sourceEventStore);

        var request = new AddEventStoreSubscriptions
        {
            TargetEventStore = eventStore.Name,
            Subscriptions =
            [
                new ContractEventStoreSubscriptionDefinition
                {
                    Identifier = definition.Id.Value,
                    SourceEventStore = definition.SourceEventStore,
                    EventTypes = definition.EventTypes.Select(et => new ContractEventType { Id = et.Value, Generation = 1 }).ToList()
                }
            ]
        };

        await _servicesAccessor.Services.EventStoreSubscriptions.Add(request);
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(EventStoreSubscriptionId subscriptionId)
    {
        logger.Unsubscribing(subscriptionId);

        var request = new RemoveEventStoreSubscriptions
        {
            TargetEventStore = eventStore.Name,
            SubscriptionIds = [subscriptionId.Value]
        };

        await _servicesAccessor.Services.EventStoreSubscriptions.Remove(request);
    }
}
