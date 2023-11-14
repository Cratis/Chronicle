// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/> using a reducer.
/// </summary>
public class ReducerAggregateRootStateProvider : IAggregateRootStateProvider
{
    readonly AggregateRoot _aggregateRoot;
    readonly IReducerHandler _reducer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerAggregateRootStateProvider"/> class.
    /// </summary>
    /// <param name="aggregateRoot">The <see cref="AggregateRoot"/> the state is for.</param>
    /// <param name="reducer"><see cref="IReducerHandler"/> to use for creating the state.</param>
    public ReducerAggregateRootStateProvider(AggregateRoot aggregateRoot, IReducerHandler reducer)
    {
        _aggregateRoot = aggregateRoot;
        _reducer = reducer;
    }

    /// <inheritdoc/>
    public async Task<object?> Provide()
    {
        var events = await _aggregateRoot.EventSequence.GetForEventSourceIdAndEventTypes(_aggregateRoot._eventSourceId, _aggregateRoot.EventHandlers.EventTypes);
        var result = await _reducer.OnNext(events, null);
        return result.State;
    }

    /// <inheritdoc/>
    public async Task<object?> Update(object? initialState, IEnumerable<object> events)
    {
        var eventsWithContext = events.Select(_ => new EventAndContext(_, EventContext.EmptyWithEventSourceId(_aggregateRoot._eventSourceId)));
        var result = await _reducer.Invoker.Invoke(eventsWithContext, initialState);
        return result.State;
    }
}
