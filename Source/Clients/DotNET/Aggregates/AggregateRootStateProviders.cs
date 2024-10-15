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
/// <param name="reducers">All the reducers in the system.</param>
/// <param name="projections"><see cref="IProjections"/> to possibly get state from.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public class AggregateRootStateProviders(
    IReducers reducers,
    IProjections projections,
    IServiceProvider serviceProvider) : IAggregateRootStateProviders
{
    /// <inheritdoc/>
    public Task<IAggregateRootStateProvider<TState>> CreateFor<TState>(IAggregateRootContext aggregateRootContext)
    {
        var stateType = typeof(TState);
        var hasReducer = reducers.HasReducerFor(stateType);
        var hasProjection = projections.HasFor(stateType);

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
            var reducer = reducers.GetForModelType(stateType);
            return Task.FromResult<IAggregateRootStateProvider<TState>>(new ReducerAggregateRootStateProvider<TState>(aggregateRootContext, reducer, serviceProvider));
        }

        return Task.FromResult<IAggregateRootStateProvider<TState>>(new ProjectionAggregateRootStateProvider<TState>(aggregateRootContext, projections));
    }
}
