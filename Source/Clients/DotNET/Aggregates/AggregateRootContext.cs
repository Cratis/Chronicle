// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootContext"/>.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the context.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> for the context.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the context.</param>
/// <param name="aggregateRoot">The <see cref="IAggregateRoot"/> for the context.</param>
/// <param name="unitOfWork">The <see cref="IUnitOfWork"/> for the context.</param>
/// <param name="hasEvents">A value indicating whether or not the context has events.</param>
public class AggregateRootContext(
    CorrelationId correlationId,
    EventSourceId eventSourceId,
    IEventSequence eventSequence,
    IAggregateRoot aggregateRoot,
    IUnitOfWork unitOfWork,
    bool hasEvents = false) : IAggregateRootContext
{
    /// <inheritdoc/>
    public CorrelationId CorrelationId { get; } = correlationId;

    /// <inheritdoc/>
    public EventSourceId EventSourceId { get; } = eventSourceId;

    /// <inheritdoc/>
    public IEventSequence EventSequence { get; } = eventSequence;

    /// <inheritdoc/>
    public IAggregateRoot AggregateRoot { get; } = aggregateRoot;

    /// <inheritdoc/>
    public IUnitOfWork UnitOfWOrk { get; } = unitOfWork;

    /// <inheritdoc/>
    public bool HasEvents { get; internal set; } = hasEvents;
}
