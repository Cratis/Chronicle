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
    /// Clears every retained per-value claim in a constraint's scope before rebuilding its index.
    /// </summary>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scopeKey">The resolved scope to clear.</param>
    /// <returns>Awaitable task.</returns>
    Task ClearValues(UniqueConstraintDefinition definition, string scopeKey = "");

    /// <summary>
    /// Saves a constraint value using its retention mode.
    /// </summary>
    /// <param name="eventSourceId">The claiming event source.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">Optional resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "");

    /// <summary>
    /// Releases every value held by the source for this constraint and scope.
    /// </summary>
    /// <param name="eventSourceId">The owning event source.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scopeKey">Optional resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "");

    /// <summary>
    /// Releases one retained value only if the event source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owning event source.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="value">The hashed value to release.</param>
    /// <param name="scopeKey">Optional resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "");

    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check for.</param>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to check for.</param>
    /// <param name="value"><see cref="UniqueConstraintValue"/>to check.</param>
    /// <param name="scopeKey">Optional scope key for scoped constraints.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "");
}
