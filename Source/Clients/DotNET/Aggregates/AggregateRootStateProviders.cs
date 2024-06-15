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
/// <param name="immediateProjections"><see cref="IImmediateProjections"/> to possibly get state from.</param>
public class AggregateRootStateProviders(
    IReducersRegistrar reducersRegistrar,
    IImmediateProjections immediateProjections) : IAggregateRootStateProviders
{
    /// <inheritdoc/>
    public Task<IAggregateRootStateProvider> CreateFor(AggregateRoot aggregateRoot)
    {
        if (!aggregateRoot.IsStateful)
        {
            return Task.FromResult<IAggregateRootStateProvider>(NullAggregateRootStateProvider.Instance);
        }

        var hasReducer = reducersRegistrar.HasReducerFor(aggregateRoot.StateType);
        var hasProjection = immediateProjections.HasProjectionFor(aggregateRoot.StateType);

        if (!hasReducer && !hasProjection)
        {
            throw new MissingAggregateRootStateProvider(aggregateRoot.GetType());
        }

        if (hasReducer && hasProjection)
        {
            throw new AmbiguousAggregateRootStateProvider(aggregateRoot.GetType());
        }

        if (hasReducer)
        {
            var reducer = reducersRegistrar.GetForModelType(aggregateRoot.StateType);
            return Task.FromResult<IAggregateRootStateProvider>(new ReducerAggregateRootStateProvider(aggregateRoot, reducer));
        }

        return Task.FromResult<IAggregateRootStateProvider>(new ProjectionAggregateRootStateProvider(aggregateRoot, immediateProjections));
    }
}
