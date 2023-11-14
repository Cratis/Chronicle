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
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to initialize.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    /// <param name="events">Optional events to initialize the aggregate root with.</param>
    /// <returns>The aggregate root for continuation.</returns>
    public static AggregateRoot Initialize(this AggregateRoot aggregateRoot, EventSourceId eventSourceId, IEnumerable<object>? events = default)
    {
        aggregateRoot.EventHandlers = new AggregateRootEventHandlers(aggregateRoot.GetType());
        aggregateRoot.EventSequence = new NullEventSequence();
        aggregateRoot.CausationManager = new NullCausationManager();
        aggregateRoot._eventSourceId = eventSourceId;

        if (events?.Count() > 0)
        {
            aggregateRoot.EventHandlers.Handle(aggregateRoot, events.Select(_ => new EventAndContext(_, EventContext.EmptyWithEventSourceId(eventSourceId))));
        }

        aggregateRoot.InternalOnActivate().GetAwaiter().GetResult();

        return aggregateRoot;
    }

    /// <summary>
    /// Prepare an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to initialize.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    /// <param name="state">Optional state to set for the aggregate root. If not set, this will default to null.</param>
    /// <typeparam name="TState">Type of state to set.</typeparam>
    /// <returns>The aggregate root for continuation.</returns>
    public static AggregateRoot<TState> Initialize<TState>(this AggregateRoot<TState> aggregateRoot, EventSourceId eventSourceId, TState? state = null)
        where TState : class
    {
        aggregateRoot.Initialize(eventSourceId);
        aggregateRoot.MutateState(state);

        aggregateRoot.InternalOnActivate().GetAwaiter().GetResult();

        return aggregateRoot;
    }

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
        aggregateRoot.MutateState(state);
        return aggregateRoot;
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
