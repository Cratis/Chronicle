// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Validates exact subscription generations without accepting a storage provider's substitute schema.
/// </summary>
public static class EventStoreSubscriptionGenerationValidation
{
    /// <summary>
    /// Validates that a subscription pins at least one type and no type pins multiple generations.
    /// </summary>
    /// <param name="pins">The subscription pins.</param>
    /// <exception cref="EventStoreSubscriptionHasNoGenerationPins">The subscription has no pins.</exception>
    /// <exception cref="ObserverPinsMultipleGenerationsOfEventType">One type pins several generations.</exception>
    public static void ValidatePins(IEnumerable<EventType> pins)
    {
        var types = pins.ToArray();
        if (types.Length == 0)
        {
            throw new EventStoreSubscriptionHasNoGenerationPins();
        }

        var duplicate = types.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(pin => pin.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }
    }

    /// <summary>
    /// Reads a registered generation only if it exists in the complete definition and the returned schema matches it.
    /// </summary>
    /// <param name="eventTypes">The event type storage for the store being validated.</param>
    /// <param name="pin">The exact generation to validate.</param>
    /// <returns>The exact schema.</returns>
    /// <exception cref="EventTypeGenerationNotRegisteredForObserver">The store does not have the exact generation.</exception>
    public static async Task<EventTypeSchema> GetExactSchema(IEventTypesStorage eventTypes, EventType pin)
    {
        if (!await eventTypes.HasFor(pin.Id, pin.Generation) || !(await eventTypes.GetDefinition(pin.Id)).Generations.Any(_ => _.Generation == pin.Generation))
        {
            throw new EventTypeGenerationNotRegisteredForObserver(pin);
        }

        var schema = await eventTypes.GetFor(pin.Id, pin.Generation);
        if (schema.Type.Id != pin.Id || schema.Type.Generation != pin.Generation)
        {
            throw new EventTypeGenerationNotRegisteredForObserver(pin);
        }

        return schema;
    }
}
