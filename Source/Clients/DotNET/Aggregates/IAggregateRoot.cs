// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines an aggregate root.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Apply a single event to the aggregate root.
    /// </summary>
    /// <typeparam name="T">Type of event to apply.</typeparam>
    /// <param name="event">Event to apply.</param>
    /// <returns>Awaitable task.</returns>
    Task Apply<T>(T @event)
        where T : class;

    /// <summary>
    /// Commits the aggregate root.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<AggregateRootCommitResult> Commit();
}
