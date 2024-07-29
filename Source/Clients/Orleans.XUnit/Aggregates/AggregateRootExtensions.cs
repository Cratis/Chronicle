// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Extension methods for testing with <see cref="AggregateRoot"/>.
/// </summary>
public static class AggregateRootExtensions
{
    /// <summary>
    /// Mutate the state of an <see cref="AggregateRoot{TState}"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to set state for.</param>
    /// <param name="state">State to set.</param>
    /// <typeparam name="TState">Type of state to set.</typeparam>
    /// <returns>The aggregate root for continuation.</returns>
    public static AggregateRoot<TState> SetState<TState>(this AggregateRoot<TState> aggregateRoot, TState state)
        where TState : class
    {
        aggregateRoot._state?.SetState(state);
        return aggregateRoot;
    }

    /// <summary>
    /// Get the uncommitted events for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to get for.</param>
    /// <returns>A collection of the actual events applied.</returns>
    public static IImmutableList<object> GetUncommittedEvents(this AggregateRoot aggregateRoot)
    {
        return aggregateRoot._mutation?.UncommittedEvents.ToImmutableList() ?? [];
    }

    /// <summary>
    /// Get the uncommitted events for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to get for.</param>
    /// <typeparam name="TState">Type of state to set.</typeparam>
    /// <returns>A collection of the actual events applied.</returns>
    public static IImmutableList<object> GetUncommittedEvents<TState>(this AggregateRoot<TState> aggregateRoot)
        where TState : class
    {
        return aggregateRoot._mutation?.UncommittedEvents.ToImmutableList() ?? [];
    }
}
