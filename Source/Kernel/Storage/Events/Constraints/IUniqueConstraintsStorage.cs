// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for unique constraints.
/// </summary>
public interface IUniqueConstraintsStorage
{
    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check for.</param>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to check for.</param>
    /// <param name="value"><see cref="UniqueConstraintValue"/>to check.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value);

    /// <summary>
    /// Save a constraint value.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to save for.</param>
    /// <param name="name"><see cref="ConstraintName"/> to save for.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> the value exists at.</param>
    /// <param name="value"><see cref="UniqueConstraintValue"/>to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value);

    /// <summary>
    /// Remove a constraint value.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to remove for.</param>
    /// <param name="name"><see cref="ConstraintName"/> to remove for.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(EventSourceId eventSourceId, ConstraintName name);
}
