// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootContext"/>.
/// </summary>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> for the context.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the context.</param>
/// <param name="aggregateRoot">The <see cref="IAggregateRoot"/> for the context.</param>
/// <param name="unitOfWork">The <see cref="IUnitOfWork"/> for the context.</param>
/// <param name="nextSequenceNumber">The next <see cref="EventSequenceNumber"/>.</param>
public class AggregateRootContext(
    EventSourceId eventSourceId,
    IEventSequence eventSequence,
    IAggregateRoot aggregateRoot,
    IUnitOfWork unitOfWork,
    EventSequenceNumber nextSequenceNumber) : IAggregateRootContext
{
    /// <inheritdoc/>
    public EventSourceId EventSourceId { get; } = eventSourceId;

    /// <inheritdoc/>
    public IEventSequence EventSequence { get; } = eventSequence;

    /// <inheritdoc/>
    public IAggregateRoot AggregateRoot { get; } = aggregateRoot;

    /// <inheritdoc/>
    public IUnitOfWork UnitOfWOrk { get; } = unitOfWork;

    /// <inheritdoc/>
    public EventSequenceNumber NextSequenceNumber { get; set; } = nextSequenceNumber;

    /// <inheritdoc/>
    public bool HasEvents { get; set; } = nextSequenceNumber != EventSequenceNumber.First;
}
