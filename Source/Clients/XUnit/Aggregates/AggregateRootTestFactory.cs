// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.XUnit;
using Cratis.Chronicle.XUnit.Events;
using Cratis.Execution;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents a factory for creating <see cref="AggregateRoot"/> instances for testing.
/// </summary>
public static class AggregateRootTestFactory
{
    /// <summary>
    /// Create an instance of an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    /// <param name="dependencies">Optional dependencies to pass to the constructor.</param>
    /// <typeparam name="TAggregateRoot">Type of <see cref="AggregateRoot"/> to create.</typeparam>
    /// <returns>An instance of the <see cref="AggregateRoot"/> for testing.</returns>
    public static TAggregateRoot Create<TAggregateRoot>(EventSourceId eventSourceId, params object[] dependencies)
        where TAggregateRoot : AggregateRoot
    {
        var aggregateRoot = (Activator.CreateInstance(typeof(TAggregateRoot), dependencies) as TAggregateRoot)!;
        var eventSequence = new EventSequenceForTesting(Defaults.Instance.EventTypes);
#pragma warning disable CA2000 // Call dispose
        var unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, Defaults.Instance.EventStore);
#pragma warning restore CA2000 // Call dispose

        var aggregateRootContext = new AggregateRootContext(
            EventSourceType.Default,
            eventSourceId,
            aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            eventSequence,
            aggregateRoot,
            unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.Unavailable);

        var mutator = new AggregateRootMutatorForTesting();
        var mutation = new AggregateRootMutation(aggregateRootContext, mutator, eventSequence);
        aggregateRoot._context = aggregateRootContext;
        aggregateRoot._mutation = mutation;
        return aggregateRoot;
    }

    /// <summary>
    /// Create an instance of an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> for the aggregate root.</param>
    /// <param name="dependencies">Optional dependencies to pass to the constructor.</param>
    /// <typeparam name="TAggregateRoot">Type of <see cref="AggregateRoot"/> to create.</typeparam>
    /// <typeparam name="TState">Type of state for the aggregate root.</typeparam>
    /// <returns>An instance of the <see cref="AggregateRoot"/> for testing.</returns>
    public static AggregateRoot<TState> Create<TAggregateRoot, TState>(EventSourceId eventSourceId, params object[] dependencies)
        where TAggregateRoot : AggregateRoot<TState>
    {
        var aggregateRoot = (Activator.CreateInstance(typeof(TAggregateRoot), dependencies) as AggregateRoot<TState>)!;
        var eventSequence = new EventSequenceForTesting(Defaults.Instance.EventTypes);
#pragma warning disable CA2000 // Call dispose
        var unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, Defaults.Instance.EventStore);
#pragma warning restore CA2000 // Call dispose

        var aggregateRootContext = new AggregateRootContext(
            EventSourceType.Default,
            eventSourceId,
            aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            eventSequence,
            aggregateRoot,
            unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.Unavailable);

        var mutator = new AggregateRootMutatorForTesting();
        var mutation = new AggregateRootMutation(aggregateRootContext, mutator, eventSequence);
        aggregateRoot._context = aggregateRootContext;
        aggregateRoot._mutation = mutation;
        aggregateRoot._state = new AggregateRootState<TState>();
        return aggregateRoot;
    }
}
