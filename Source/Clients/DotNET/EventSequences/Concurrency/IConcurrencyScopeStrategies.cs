// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Defines a system for managing concurrency strategies for event sequences.
/// </summary>
public interface IConcurrencyScopeStrategies
{
    /// <summary>
    /// Gets the appropriate <see cref="IConcurrencyScopeStrategy"/> for the given <see cref="IEventSequence"/>.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> for which to get the concurrency scope strategy.</param>
    /// <returns>An instance of <see cref="IConcurrencyScopeStrategy"/> that can be used to manage concurrency for the specified event sequence.</returns>
    IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence);

    /// <summary>
    /// Gets the appropriate <see cref="IConcurrencyScopeStrategy"/> for the given <see cref="IAggregateRootContext"/>.
    /// </summary>
    /// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> for which to get the concurrency scope strategy.</param>
    /// <returns>An instance of <see cref="IConcurrencyScopeStrategy"/> that can be used to manage concurrency for the specified aggregate root context.</returns>
    IConcurrencyScopeStrategy GetFor(IAggregateRootContext aggregateRootContext);
}
