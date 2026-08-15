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
    ConstraintScope? Scope = default) : IConstraintDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueEventTypeConstraintDefinition"/> class from a single removal event.
    /// </summary>
    /// <param name="name">Name of the constraint.</param>
    /// <param name="messageCallback">The callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
    /// <param name="eventTypeIds">The <see cref="EventTypeId"/> values the constraint covers.</param>
    /// <param name="removedWith">The <see cref="Events.EventTypeId"/> of the event that removes the constraint, or <see langword="null"/> for none.</param>
    /// <param name="scope">The <see cref="ConstraintScope"/> for the constraint.</param>
    /// <remarks>
    /// The signature this type had while a constraint could only be released by one event. It is kept so that an
    /// assembly compiled against that shape keeps linking: optional arguments are baked in at the call site, so
    /// every previously compiled call refers to the full argument list, which is what this restores.
    /// </remarks>
    [Obsolete("A constraint can be released by more than one event. Pass a collection of event type ids instead - this overload wraps the single value and will be removed.")]
    public UniqueEventTypeConstraintDefinition(
        ConstraintName name,
        ConstraintViolationMessageProvider messageCallback,
        IEnumerable<EventTypeId> eventTypeIds,
        EventTypeId? removedWith,
        ConstraintScope? scope)
        : this(name, messageCallback, eventTypeIds, removedWith is null ? [] : [removedWith], scope)
    {
    }
}
