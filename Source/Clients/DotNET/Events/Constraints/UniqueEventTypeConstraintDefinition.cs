// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventTypeIds">The <see cref="EventTypeId"/> values the constraint covers.</param>
/// <param name="RemovedWith">The <see cref="Events.EventTypeId"/> values of the events that remove the constraint.</param>
/// <param name="Scope">The <see cref="ConstraintScope"/> for the constraint.</param>
/// <remarks>
/// The constraint allows at most one event drawn from the covered event types per event source. Declare several
/// event types under one constraint name to make them mutually exclusive — an event source that is terminal
/// through either of two outcomes can then have one of them, never both and never twice.
/// <para>
/// More than one event type can release the constraint, because a cycle can end in more than one way. Each one
/// releases it on its own, so the next cycle is allowed after whichever of them was appended most recently.
/// </para>
/// </remarks>
public record UniqueEventTypeConstraintDefinition(
    ConstraintName Name,
    ConstraintViolationMessageProvider MessageCallback,
    IEnumerable<EventTypeId> EventTypeIds,
    IEnumerable<EventTypeId> RemovedWith,
    ConstraintScope? Scope = default) : IConstraintDefinition;
