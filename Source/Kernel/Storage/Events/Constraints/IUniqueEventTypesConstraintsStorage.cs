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
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the existing event, if any.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Exists(EventType eventType, EventSourceId eventSourceId, out EventSequenceNumber sequenceNumber);
}
