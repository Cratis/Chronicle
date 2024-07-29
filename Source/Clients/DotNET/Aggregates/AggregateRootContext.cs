// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootContext"/>.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the context.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> for the context.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the context.</param>
/// <param name="aggregateRoot">The <see cref="IAggregateRoot"/> for the context.</param>
/// <param name="autoCommit">A value indicating whether or not to automatically commit changes on every apply.</param>
/// <param name="hasEvents">A value indicating whether or not the context has events.</param>
public class AggregateRootContext(
    CorrelationId correlationId,
    EventSourceId eventSourceId,
    IEventSequence eventSequence,
    IAggregateRoot aggregateRoot,
    bool autoCommit,
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
    public bool AutoCommit { get; } = autoCommit;

    /// <inheritdoc/>
    public bool HasEvents { get; internal set; } = hasEvents;
}
