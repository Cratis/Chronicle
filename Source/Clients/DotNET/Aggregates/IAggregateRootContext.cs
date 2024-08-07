// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines the context for an aggregate root.
/// </summary>
public interface IAggregateRootContext
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the context.
    /// </summary>
    CorrelationId CorrelationId { get; }

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
    /// Gets a value indicating whether or not to automatically commit changes on every apply.
    /// </summary>
    bool AutoCommit { get; }

    /// <summary>
    /// Gets a value indicating whether or not the context has events.
    /// </summary>
    bool HasEvents { get; }
}
