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
    /// Gets the <see cref="EventSourceId"/> for the context.
    /// </summary>
    EventSourceId EventSourceId { get; }

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
    /// Gets the last processed <see cref="EventSequenceNumber"/>.
    /// </summary>
    EventSequenceNumber NextSequenceNumber { get; }

    /// <summary>
    /// Gets or sets a value indicating whether there are events available for rehydration.
    /// </summary>
    bool HasEventsForRehydration { get; set; }
}
