// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider{TState}"/> using a reducer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerAggregateRootStateProvider{TState}"/> class.
/// </remarks>
/// <typeparam name="TState">Type of state for the aggregate root.</typeparam>
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> the state is for.</param>
/// <param name="reducer"><see cref="IReducerHandler"/> to use for creating the state.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to create the actual instance of the reducer.</param>
public class ReducerAggregateRootStateProvider<TState>(
    IAggregateRootContext aggregateRootContext,
    IReducerHandler reducer,
    IServiceProvider serviceProvider) : IAggregateRootStateProvider<TState>
{
    /// <inheritdoc/>
    public async Task<TState?> Provide()
    {
        var events = await aggregateRootContext.EventSequence.GetForEventSourceIdAndEventTypes(
            aggregateRootContext.EventSourceId,
            reducer.EventTypes,
            aggregateRootContext.EventStreamType,
            aggregateRootContext.EventStreamId);
        var result = await reducer.OnNext(events, null, serviceProvider);
        aggregateRootContext.TailEventSequenceNumber = events[^1].Context.SequenceNumber;
        return (TState?)result.ReadModelState;
    }

    /// <inheritdoc/>
    public async Task<TState?> Update(TState? initialState, IEnumerable<object> events)
    {
        var eventsWithContext = events.Select(_ => new EventAndContext(_, EventContext.EmptyWithEventSourceId(aggregateRootContext.EventSourceId)));
        var result = await reducer.Invoker.Invoke(serviceProvider, eventsWithContext, initialState);
        return (TState?)result.ReadModelState;
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
