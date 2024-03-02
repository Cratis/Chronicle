// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines an aggregate root.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets whether or not the aggregate root is stateful.
    /// </summary>
    public bool IsStateful { get; }

    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> for the aggregate root.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Apply a single event to the aggregate root.
    /// </summary>
    /// <typeparam name="T">Type of event to apply.</typeparam>
    /// <param name="event">Event to apply.</param>
    void Apply<T>(T @event)
        where T : class;

    /// <summary>
    /// Commits the aggregate root.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<AggregateRootCommitResult> Commit();
}
