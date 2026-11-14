// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Pure rules about what makes a subscription definition valid and comparable, independent of any observer state.
/// </summary>
internal static class ObserverSubscriptionRules
{
    /// <summary>
    /// Throws if the event types pin more than one generation of the same event type.
    /// </summary>
    /// <param name="eventTypes">The event types the observer pins.</param>
    /// <exception cref="ObserverPinsMultipleGenerationsOfEventType">Thrown when one event type is pinned at several generations.</exception>
    public static void ThrowIfPinsMultipleGenerations(IEnumerable<EventType> eventTypes)
    {
        var duplicate = eventTypes.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(type => type.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }
    }

    /// <summary>
    /// Compares two sets of filters structurally.
    /// </summary>
    /// <param name="left">The first <see cref="ObserverFilters"/>, if any.</param>
    /// <param name="right">The second <see cref="ObserverFilters"/>, if any.</param>
    /// <returns>True if both are the same filters, false if not.</returns>
    public static bool FiltersAreEqual(ObserverFilters? left, ObserverFilters? right)
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        // ObserverFilters is a record, but its Tags collection makes the generated equality a
        // reference comparison - two identical registrations from different client instances
        // would never be considered equal. Compare structurally instead.
        return left.Tags.ToHashSet().SetEquals(right.Tags) &&
               Equals(left.EventSourceType, right.EventSourceType) &&
               Equals(left.EventStreamType, right.EventStreamType);
    }

    /// <summary>
    /// Gets who owns an observer served by a subscriber of the given type.
    /// </summary>
    /// <param name="subscriberType">The type of the <see cref="IObserverSubscriber"/>.</param>
    /// <returns>The <see cref="ObserverOwner"/>.</returns>
    public static ObserverOwner OwnerFor(Type subscriberType) => subscriberType switch
    {
        Type t when t.IsAssignableTo(typeof(IAmOwnedByClient)) => ObserverOwner.Client,
        Type t when t.IsAssignableTo(typeof(IAmOwnedByKernel)) => ObserverOwner.Kernel,
        _ => ObserverOwner.None
    };
}
