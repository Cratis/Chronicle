// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

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
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/> for the event, will default to NotSet if not set.</param>
    /// <param name="tags">Optional dynamic tags to associate with the event.</param>
    /// <param name="occurred">Optional timestamp for when the event occurred.</param>
    /// <param name="subject">Optional subject identifying what the event is about.</param>
    void AddEvent(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        object @event,
        Causation causation,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default);

    /// <summary>
    /// Adds an ordered batch of events and independently labeled concurrency scopes to the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> for the events.</param>
    /// <param name="events">The events to add, in commit order.</param>
    /// <param name="concurrencyScopes">The concurrency scopes to validate with the batch.</param>
    /// <remarks>
    /// Both inputs are materialized before this method returns. Consecutive calls to <see cref="AddEvent"/> retain
    /// their legacy source-grouped order. Each call to this method forms a globally ordered segment between those
    /// legacy segments, and commit flattens every segment into one append operation. A concurrency-scope key can be
    /// an independent label when its scope does not narrow by event-source ID. For event-target labels, a missing or
    /// <see cref="ConcurrencyScope.NotSet"/> scope retains the configured strategy. An independent non-target label
    /// must use a concrete exact scope or <see cref="ConcurrencyScope.None"/>.
    /// </remarks>
    /// <exception cref="ConcurrencyScopeLabelMustBeSpecified">Thrown when an event target or scope label is unspecified, blank, or whitespace.</exception>
    /// <exception cref="ConcurrencyScopeEventSourceIdDoesNotMatchLabel">Thrown when a scope narrows by an event source different from its label.</exception>
    /// <exception cref="DuplicateConcurrencyScopeForEventSourceId">Thrown when more than one scope has the same label in one enrollment.</exception>
    /// <exception cref="ConflictingConcurrencyScopesForLabel">Thrown when a label already has a different explicit scope in this unit of work.</exception>
    /// <exception cref="IndependentConcurrencyScopeMustBeExplicit">Thrown when an independent non-target label does not have a concrete exact scope or <see cref="ConcurrencyScope.None"/>.</exception>
    /// <exception cref="UnitOfWorkCannotSpanEventSequences">Thrown when this unit of work is already bound to another event sequence.</exception>
    /// <exception cref="UnitOfWorkBatchEnrollmentNotSupported">Thrown when an alternate unit-of-work implementation does not support ordered batch enrollment.</exception>
    void AddEvents(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes) =>
        throw new UnitOfWorkBatchEnrollmentNotSupported(GetType());

    /// <summary>
    /// Get the events that have occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of events.</returns>
    IEnumerable<object> GetEvents();

    /// <summary>
    /// Gets any constraint violations that occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="ConstraintViolation"/>.</returns>
    IEnumerable<ConstraintViolation> GetConstraintViolations();

    /// <summary>
    /// Gets any concurrency violations thar occurred in the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <returns>A collection of <see cref="ConcurrencyViolation"/>.</returns>
    IEnumerable<ConcurrencyViolation> GetConcurrencyViolations();

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
    bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber);
}
