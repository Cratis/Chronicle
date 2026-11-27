// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what is known about whether an event the observer observes is waiting at or after its cursor.
/// </summary>
/// <param name="Kind">What is known.</param>
/// <param name="Position">The first such event, when <paramref name="Kind"/> says one is waiting.</param>
public readonly record struct ObserverLagRelevance(ObserverLagRelevanceKind Kind, EventSequenceNumber Position)
{
    /// <summary>
    /// Gets the relevance of an event whose presence could not be determined. The default.
    /// </summary>
    public static ObserverLagRelevance Unknown => default;

    /// <summary>
    /// Gets the relevance of a lookup that proved no event the observer observes is waiting.
    /// </summary>
    public static ObserverLagRelevance None => new(ObserverLagRelevanceKind.None, EventSequenceNumber.Unavailable);

    /// <summary>
    /// Creates the relevance of an event the observer observes that is waiting.
    /// </summary>
    /// <param name="position">The event's sequence number.</param>
    /// <returns>The <see cref="ObserverLagRelevance"/>.</returns>
    public static ObserverLagRelevance Pending(EventSequenceNumber position) => new(ObserverLagRelevanceKind.Pending, position);
}
