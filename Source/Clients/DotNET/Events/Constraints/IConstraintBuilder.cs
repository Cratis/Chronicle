// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the builder for building constraints.
/// </summary>
public interface IConstraintBuilder
{
    /// <summary>
    /// Scope the constraint per event source type.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder PerEventSourceType();

    /// <summary>
    /// Scope the constraint per event stream type.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder PerEventStreamType();

    /// <summary>
    /// Scope the constraint per event stream identifier.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder PerEventStreamId();

    /// <summary>
    /// Apply the constraints only to specific event sequences.
    /// </summary>
    /// <param name="eventSequenceIds">The <see cref="EventSequenceId"/> values of the event sequences the constraints apply to.</param>
    /// <returns>Builder for continuation.</returns>
    /// <remarks>
    /// By default a constraint applies to every event sequence its event types are appended to, and every sequence
    /// keeps an index of its own. The kernel neither validates nor indexes a constraint for an event sequence it does
    /// not apply to. That matters when the same event type is forwarded to another sequence, such as the outbox: the
    /// forwarded copy would otherwise claim the value in the outbox's own index, which only removal events appended
    /// to the outbox could release.
    /// <para>
    /// It applies to every constraint defined on this builder, wherever in the chain it is called, and calling it
    /// again adds to the event sequences already declared.
    /// </para>
    /// </remarks>
    IConstraintBuilder ForEventSequences(params EventSequenceId[] eventSequenceIds);

    /// <summary>
    /// Apply the constraints only to the event log.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    /// <remarks>
    /// Shorthand for <see cref="ForEventSequences"/> with <see cref="EventSequenceId.Log"/>. Uniqueness is usually a
    /// rule of the event log, and other sequences such as the outbox carry facts that were already validated there.
    /// </remarks>
    IConstraintBuilder ForEventLog();

    /// <summary>
    /// Start building a unique constraint.
    /// </summary>
    /// <param name="callback">Callback with <see cref="IUniqueConstraintBuilder"/> for building.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback);

    /// <summary>
    /// Adds a unique constraint for a specific event type. This means there can only be one instance of this event type per event source identifier.
    /// </summary>
    /// <typeparam name="TEventType">Type of event to add for.</typeparam>
    /// <param name="message">Optional message for the constraint.</param>
    /// <param name="name">Optional name for the constraint.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique<TEventType>(ConstraintViolationMessage? message = default, ConstraintName? name = default);

    /// <summary>
    /// Adds a unique constraint for a specific event type. This means there can only be one instance of this event type per event source identifier.
    /// </summary>
    /// <typeparam name="TEventType">Type of event to add for.</typeparam>
    /// <param name="messageCallback">Callback for providing message for the constraint.</param>
    /// <param name="name">Optional name for the constraint.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique<TEventType>(ConstraintViolationMessageProvider messageCallback, ConstraintName? name = default);

    /// <summary>
    /// Indicate an event that releases the unique event type constraint most recently declared on this builder.
    /// </summary>
    /// <typeparam name="TRemovalEventType">Type of event that releases the constraint.</typeparam>
    /// <returns>Builder for continuation.</returns>
    /// <exception cref="NoUniqueEventTypeConstraintToRemove">Thrown when no unique event type constraint has been declared on the builder yet.</exception>
    /// <remarks>
    /// Without this the constraint can only say "at most one, forever". Most lifecycles cycle, so declare the event
    /// that ends a cycle and the covered event types are allowed again for the next one — a covered event only
    /// violates the constraint when it comes after the most recent removal event on the same event source.
    /// <para>
    /// It applies to the <see cref="Unique{TEventType}(ConstraintViolationMessage, ConstraintName)"/> declaration it
    /// follows, which is why it belongs on the same chain. Call it as many times as the lifecycle has terminal
    /// facts: every declared event type releases the constraint on its own, and declaring a second does not replace
    /// the first. Declaring several event types under one constraint name makes them one constraint, which is
    /// released by every removal event declared for that name.
    /// </para>
    /// </remarks>
    IConstraintBuilder RemovedWith<TRemovalEventType>();

    /// <summary>
    /// Declare an event that closes a stream scope, configured inside the callback.
    /// </summary>
    /// <typeparam name="TEvent">The closing event type.</typeparam>
    /// <param name="configure">Optional scope and reopening configuration.</param>
    /// <param name="name">Optional owner name, defaulting to the event type identifier.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="ClosesStreamConstraintsNotSupported">The implementation does not support closing events.</exception>
    /// <exception cref="PropertyDoesNotExistOnEventType">A closing or reopening type lacks the configured property.</exception>
    /// <exception cref="MissingNameForClosesStreamConstraint">The configured owner name is reserved for manual closures.</exception>
    IConstraintBuilder ClosesStreamOn<TEvent>(Action<IClosesStreamBuilder<TEvent>>? configure = default, ConstraintName? name = default) =>
        throw new ClosesStreamConstraintsNotSupported();

    /// <summary>
    /// Add a constraint to the builder.
    /// </summary>
    /// <param name="constraint"><see cref="Constraint"/> to add.</param>
    void AddConstraint(IConstraintDefinition constraint);

    /// <summary>
    /// Build the constraint.
    /// </summary>
    /// <returns>A collection of <see cref="IConstraintDefinition"/> to use for registering with server.</returns>
    IImmutableList<IConstraintDefinition> Build();
}
