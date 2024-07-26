// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProviders"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRootStateProviders"/> class.
/// </remarks>
/// <param name="reducersRegistrar">All the reducers in the system.</param>
/// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for getting <see cref="IAggregateRootEventHandlers"/>.</param>
/// <param name="projections"><see cref="IProjections"/> to possibly get state from.</param>
public class AggregateRootStateProviders(
    IReducers reducersRegistrar,
    IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
    IProjections projections) : IAggregateRootStateProviders
{
    /// <inheritdoc/>
    public Task<IAggregateRootStateProvider<TState>> CreateFor<TState>(IAggregateRootContext aggregateRootContext)
        where TState : class
    {
        var stateType = typeof(TState);
        var hasReducer = reducersRegistrar.HasReducerFor(stateType);
        var hasProjection = projections.HasProjectionFor(stateType);

        if (!hasReducer && !hasProjection)
        {
            throw new MissingAggregateRootStateProvider(aggregateRootContext.AggregateRoot.GetType());
        }

        if (hasReducer && hasProjection)
        {
            throw new AmbiguousAggregateRootStateProvider(aggregateRootContext.AggregateRoot.GetType());
        }

        if (hasReducer)
        {
            var reducer = reducersRegistrar.GetForModelType(stateType);
            return Task.FromResult<IAggregateRootStateProvider<TState>>(new ReducerAggregateRootStateProvider<TState>(aggregateRootContext, aggregateRootEventHandlersFactory, reducer));
        }

        return Task.FromResult<IAggregateRootStateProvider<TState>>(new ProjectionAggregateRootStateProvider<TState>(aggregateRootContext, projections));
    }
}
