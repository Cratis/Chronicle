// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents a unit of work.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.
    /// </summary>
    CorrelationId CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether or not the <see cref="IUnitOfWork"/> was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Add an event that has occurred to the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> for the event.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> for the event.</param>
    /// <param name="event">The event that has occurred.</param>
    /// <param name="causation">The <see cref="Causation"/> for the event.</param>
    void AddEvent(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation);

    /// <summary>
    /// Get the events that have occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of events.</returns>
    IImmutableList<object> GetEvents();

    /// <summary>
    /// Gets the constraint violations that have occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="ConstraintViolation"/>.</returns>
    IImmutableList<ConstraintViolation> GetConstraintViolations();

    /// <summary>
    /// Get any errors that have occurred while attempting to commit in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="AppendError"/>.</returns>
    IImmutableList<AppendError> GetAppendErrors();

    /// <summary>
    /// Commit the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Commit();

    /// <summary>
    /// Rollback the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rollback();
}
