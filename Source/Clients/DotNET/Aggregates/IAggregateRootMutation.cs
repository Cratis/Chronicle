// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines the mutation related to an aggregate root.
/// </summary>
public interface IAggregateRootMutation
{
    /// <summary>
    /// Gets the <see cref="EventSourceId"/> for the aggregate root.
    /// </summary>
    EventSourceId EventSourceId { get; }

    /// <summary>
    /// Gets the uncommitted events for the aggregate root.
    /// </summary>
    IImmutableList<object> UncommittedEvents { get; }

    /// <summary>
    /// Gets a value indicating whether the mutation has events. This is an indication of whether or not the aggregate root has been mutated.
    /// </summary>
    bool HasEvents { get; }

    /// <summary>
    /// Gets the mutator for the aggregate root.
    /// </summary>
    IAggregateRootMutator Mutator { get; }

    /// <summary>
    /// Apply a single event to the aggregate root mutation.
    /// </summary>
    /// <typeparam name="TEvent">Type of event to apply.</typeparam>
    /// <param name="event">Event to apply.</param>
    /// <returns>Awaitable task.</returns>
    Task Apply<TEvent>(TEvent @event)
        where TEvent : class;

    /// <summary>
    /// Commit the mutation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<AggregateRootCommitResult> Commit();
}
