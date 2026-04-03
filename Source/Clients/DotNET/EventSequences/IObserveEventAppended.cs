// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines an observer that receives notifications when events are appended directly (non-transactionally)
/// to an event sequence, regardless of whether the append succeeded or failed.
/// </summary>
/// <remarks>
/// This observer fires for every direct <see cref="IEventSequence.Append"/> and
/// <see cref="IEventSequence.AppendMany(Events.EventSourceId, System.Collections.Generic.IEnumerable{object}, Events.EventStreamType?, Events.EventStreamId?, Events.EventSourceType?, CorrelationId?, System.Collections.Generic.IEnumerable{string}?, Concurrency.ConcurrencyScope?, System.DateTimeOffset?)"/>
/// call — whether the operation succeeded, violated a constraint, hit a concurrency conflict, or encountered an error.
/// It does not fire for transactional appends that go through
/// <see cref="ITransactionalEventSequence"/> and the unit of work commit path.
/// Use this together with <see cref="IUnitOfWorkManager"/> subscriptions to get a complete
/// picture of all direct append operations and their outcomes during a unit of work.
/// </remarks>
public interface IObserveEventAppended
{
    /// <summary>
    /// Called after a single direct <see cref="IEventSequence.Append"/> call completes, whether successfully or not.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the append operation.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the event was being appended to.</param>
    /// <param name="event">The event object that was attempted to be appended.</param>
    /// <param name="causationChain">The causation chain active at the time of the append.</param>
    /// <param name="result">The <see cref="AppendResult"/> describing success, violations, or errors.</param>
    void OnAppend(CorrelationId correlationId, EventSourceId eventSourceId, object @event, IImmutableList<Causation> causationChain, AppendResult result);

    /// <summary>
    /// Called after a direct <see cref="IEventSequence.AppendMany(Events.EventSourceId, System.Collections.Generic.IEnumerable{object}, Events.EventStreamType?, Events.EventStreamId?, Events.EventSourceType?, CorrelationId?, System.Collections.Generic.IEnumerable{string}?, Concurrency.ConcurrencyScope?, System.DateTimeOffset?)"/> call completes, whether successfully or not.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the append operation.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the events were being appended to.</param>
    /// <param name="events">The event objects that were attempted to be appended.</param>
    /// <param name="causationChain">The causation chain active at the time of the append.</param>
    /// <param name="result">The <see cref="AppendManyResult"/> describing success, violations, or errors.</param>
    void OnAppendMany(CorrelationId correlationId, EventSourceId eventSourceId, IEnumerable<object> events, IImmutableList<Causation> causationChain, AppendManyResult result);
}
