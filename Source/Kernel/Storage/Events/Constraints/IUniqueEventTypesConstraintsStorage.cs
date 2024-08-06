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
    /// <param name="eventType"><see cref="EventType"/> to check.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not it exists and the <see cref="EventSequenceNumber"/> where it already exists.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    Task<(bool Exists, EventSequenceNumber SequenceNumber)> Exists(EventType eventType, EventSourceId eventSourceId);
}
