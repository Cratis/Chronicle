// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines a system that can manage <see cref="IAggregateRootStateProvider{TState}"/> for <see cref="AggregateRoot"/>.
/// </summary>
public interface IAggregateRootStateProviders
{
    /// <summary>
    /// Create a <see cref="IAggregateRootStateProvider{TState}"/> for a specific <see cref="AggregateRoot"/>.
    /// </summary>
    /// <typeparam name="TState">Type of state for the aggregate root.</typeparam>
    /// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> for the aggregate root.</param>
    /// <returns>A new <see cref="IAggregateRootStateProvider{State}"/> instance.</returns>
    /// <exception cref="MissingAggregateRootStateProvider">Thrown if it can't resolve a state provider for the aggregate root.</exception>
    /// <exception cref="AmbiguousAggregateRootStateProvider">Thrown if there are multiple state providers for the same aggregate root, this is not allowed.</exception>
    Task<IAggregateRootStateProvider<TState>> CreateFor<TState>(IAggregateRootContext aggregateRootContext)
        where TState : class;
}
