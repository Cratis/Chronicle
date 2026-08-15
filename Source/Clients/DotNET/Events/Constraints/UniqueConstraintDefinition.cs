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
    ConstraintScope? Scope = default) : IConstraintDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintDefinition"/> class from a single removal event.
    /// </summary>
    /// <param name="name">Name of the constraint.</param>
    /// <param name="messageCallback">The callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
    /// <param name="eventsWithProperties">The <see cref="EventType"/> and properties the constraint is for.</param>
    /// <param name="removedWith">The <see cref="EventTypeId"/> of the event that removes the constraint, or <see langword="null"/> for none.</param>
    /// <param name="ignoreCasing">Whether this constraint should ignore casing.</param>
    /// <param name="scope">The <see cref="ConstraintScope"/> for the constraint.</param>
    /// <remarks>
    /// The signature this type had while a constraint could only be released by one event. It is kept so that an
    /// assembly compiled against that shape keeps linking: optional arguments are baked in at the call site, so
    /// every previously compiled call refers to the full argument list, which is what this restores.
    /// </remarks>
    [Obsolete("A constraint can be released by more than one event. Pass a collection of event type ids instead - this overload wraps the single value and will be removed.")]
    public UniqueConstraintDefinition(
        ConstraintName name,
        ConstraintViolationMessageProvider messageCallback,
        IEnumerable<UniqueConstraintEventDefinition> eventsWithProperties,
        EventTypeId? removedWith,
        bool ignoreCasing,
        ConstraintScope? scope)
        : this(name, messageCallback, eventsWithProperties, removedWith is null ? [] : [removedWith], ignoreCasing, scope)
    {
    }
}
