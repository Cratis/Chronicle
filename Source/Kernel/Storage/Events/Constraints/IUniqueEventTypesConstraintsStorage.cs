// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for unique event type constraints.
/// </summary>
public interface IUniqueEventTypesConstraintsStorage
{
    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to check against.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check.</param>
    /// <param name="scopeKey">Optional scope key for scoped constraints.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    /// <remarks>
    /// An append is allowed only when the event source carries none of the covered event types. A definition covering
    /// more than one makes them mutually exclusive: the first of them to be appended blocks all of the others.
    /// <para>
    /// A definition carrying a <see cref="UniqueEventTypeConstraintDefinition.RemovedWith"/> is answered per cycle
    /// rather than forever — only a covered event appended after the most recent removal event on that event source
    /// blocks the append, so an event source that has been released is free to start the next cycle. The whole
    /// definition is passed rather than its parts so that a reader cannot answer against half of it.
    /// </para>
    /// <para>
    /// A definition may declare several removal events, because a cycle can end in more than one way. The cycle
    /// ends at the most recent of them, not at the most recent of any one of them.
    /// </para>
    /// </remarks>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "");
}
