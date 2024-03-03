// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Reducers;

namespace Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/> using a reducer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerAggregateRootStateProvider"/> class.
/// </remarks>
/// <param name="aggregateRoot">The <see cref="AggregateRoot"/> the state is for.</param>
/// <param name="reducer"><see cref="IReducerHandler"/> to use for creating the state.</param>
public class ReducerAggregateRootStateProvider(AggregateRoot aggregateRoot, IReducerHandler reducer) : IAggregateRootStateProvider
{
    /// <inheritdoc/>
    public async Task<object?> Provide()
    {
        var events = await aggregateRoot.EventSequence.GetForEventSourceIdAndEventTypes(aggregateRoot._eventSourceId, aggregateRoot.EventHandlers.EventTypes);
        var result = await reducer.OnNext(events, null);
        return result.State;
    }

    /// <inheritdoc/>
    public async Task<object?> Update(object? initialState, IEnumerable<object> events)
    {
        var eventsWithContext = events.Select(_ => new EventAndContext(_, EventContext.EmptyWithEventSourceId(aggregateRoot._eventSourceId)));
        var result = await reducer.Invoker.Invoke(eventsWithContext, initialState);
        return result.State;
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
