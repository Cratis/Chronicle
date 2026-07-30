// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for unique event type constraints.
/// </summary>
public interface IUniqueEventTypesConstraintsStorage
{
    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="eventTypeIds">The <see cref="EventType"/> values the constraint covers.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check.</param>
    /// <param name="scopeKey">Optional scope key for scoped constraints.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    /// <remarks>
    /// An append is allowed only when the event source carries none of the covered event types. Passing more
    /// than one makes them mutually exclusive: the first of them to be appended blocks all of the others.
    /// </remarks>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(IEnumerable<EventTypeId> eventTypeIds, EventSourceId eventSourceId, string scopeKey = "");
}
