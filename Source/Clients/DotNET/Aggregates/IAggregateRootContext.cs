// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines the context for an aggregate root.
/// </summary>
public interface IAggregateRootContext
{
    /// <summary>
    /// Gets the <see cref="EventSourceType"/> for the context.
    /// </summary>
    EventSourceType EventSourceType { get; }

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> for the context.
    /// </summary>
    EventSourceId EventSourceId { get; }

    /// <summary>
    /// Gets the <see cref="EventStreamType"/> for the context.
    /// </summary>
    EventStreamType EventStreamType { get; }

    /// <summary>
    /// Gets the <see cref="EventStreamId"/> for the context.
    /// </summary>
    EventStreamId EventStreamId { get; }

    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> for the context.
    /// </summary>
    IEventSequence EventSequence { get; }

    /// <summary>
    /// Gets the <see cref="IAggregateRoot"/> for the context.
    /// </summary>
    IAggregateRoot AggregateRoot { get; }

    /// <summary>
    /// Gets the <see cref="IUnitOfWork"/> for the context.
    /// </summary>
    IUnitOfWork UnitOfWOrk { get; }

    /// <summary>
    /// Gets or sets the next <see cref="EventSequenceNumber"/> to process for the aggregate root in the unit of work.
    /// </summary>
    EventSequenceNumber NextSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the tail event sequence number of the aggregate root.
    /// </summary>
    EventSequenceNumber TailEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets a value indicating whether aggregate root has events.
    /// </summary>
    bool HasEvents { get; set; }
}
