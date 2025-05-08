// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
    /// Gets the value indicating whether the unit of work is completed.
    /// </summary>
    /// <remarks>
    /// Unit of work being completed is semantically equal to it being disposed.
    /// </remarks>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.
    /// </summary>
    CorrelationId CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="IUnitOfWork"/> was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Add an event that has occurred to the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> for the event.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> for the event.</param>
    /// <param name="event">The event that has occurred.</param>
    /// <param name="causation">The <see cref="Causation"/> for the event.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> for the event, will default to the All stream if not set.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> for the event, will default to Default is not set.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> for the event, will default to Default is not set.</param>
    void AddEvent(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        object @event,
        Causation causation,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default);

    /// <summary>
    /// Get the events that have occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of events.</returns>
    IEnumerable<object> GetEvents();

    /// <summary>
    /// Gets the constraint violations that have occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="ConstraintViolation"/>.</returns>
    IEnumerable<ConstraintViolation> GetConstraintViolations();

    /// <summary>
    /// Get any errors that have occurred while attempting to commit in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="AppendError"/>.</returns>
    IEnumerable<AppendError> GetAppendErrors();

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

    /// <summary>
    /// Set callback to be called when completed.
    /// </summary>
    /// <param name="callback">The callback to call.</param>
    void OnCompleted(Action<IUnitOfWork> callback);

    /// <summary>
    /// Try to get the <see cref="EventSequenceNumber"/> of the last committed event.
    /// </summary>
    /// <param name="eventSequenceNumber">The outputted <see cref="EventSequenceNumber"/> of the last committed event.</param>
    /// <returns>True if events were committed, false if not.</returns>
    bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)]out EventSequenceNumber? eventSequenceNumber);
}
