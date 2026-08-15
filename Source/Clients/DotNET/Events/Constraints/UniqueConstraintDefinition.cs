// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventsWithProperties">The <see cref="EventType"/> and properties the constraint is for.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> values of the events that remove the constraint.</param>
/// <param name="IgnoreCasing">Whether this constraint should ignore casing.</param>
/// <param name="Scope">The <see cref="ConstraintScope"/> for the constraint.</param>
/// <remarks>
/// A lifecycle can end in more than one way, so more than one event type can release the claimed value. Each one
/// releases it on its own — the first of them appended for an event source frees the value for anyone to claim.
/// </remarks>
public record UniqueConstraintDefinition(
    ConstraintName Name,
    ConstraintViolationMessageProvider MessageCallback,
    IEnumerable<UniqueConstraintEventDefinition> EventsWithProperties,
    IEnumerable<EventTypeId> RemovedWith,
    bool IgnoreCasing,
    ConstraintScope? Scope = default) : IConstraintDefinition;
