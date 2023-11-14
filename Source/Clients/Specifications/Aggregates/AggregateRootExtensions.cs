// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Aggregates;
using Aksio.Cratis.Events;
using Aksio.Cratis.Specifications.Auditing;
using Aksio.Cratis.Specifications.Events;

namespace Aksio.Cratis.Specifications.Aggregates;

/// <summary>
/// Extension methods for testing with <see cref="AggregateRoot"/>.
/// </summary>
public static class AggregateRootExtensions
{
    /// <summary>
    /// Prepare an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to prepare.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    public static void Prepare(this AggregateRoot aggregateRoot, EventSourceId eventSourceId)
    {
        aggregateRoot.EventHandlers = new AggregateRootEventHandlers(aggregateRoot.GetType());
        aggregateRoot.EventSequence = new NullEventSequence();
        aggregateRoot.CausationManager = new NullCausationManager();
        aggregateRoot._eventSourceId = eventSourceId;
    }

    /// <summary>
    /// Prepare an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to prepare.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    /// <param name="state">State to set for the aggregate root.</param>
    /// <typeparam name="TState">Type of state to set.</typeparam>
    public static void Prepare<TState>(this AggregateRoot aggregateRoot, EventSourceId eventSourceId, TState state)
        where TState : class, new()
    {
        aggregateRoot.Prepare(eventSourceId);
        aggregateRoot.SetState(state);
    }

    /// <summary>
    /// Get the uncommitted events for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to get for.</param>
    /// <returns>A collection of the actual events applied.</returns>
    public static IImmutableList<object> GetUncommittedEvents(this AggregateRoot aggregateRoot)
    {
        return aggregateRoot._uncommittedEvents.ToImmutableList();
    }
}
