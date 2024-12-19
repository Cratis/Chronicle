// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines an aggregate root.
/// </summary>
public interface IAggregateRoot : IUnitOfWorkEnlistee
{
    /// <summary>
    /// Apply a single event to the aggregate root.
    /// </summary>
    /// <param name="event">Event to apply.</param>
    /// <returns>Awaitable task.</returns>
    Task Apply(object @event);

    /// <summary>
    /// Commits the aggregate root.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<AggregateRootCommitResult> Commit();
}
