// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProviders"/>.
/// </summary>
public class AggregateRootStateProviders : IAggregateRootStateProviders
{
    readonly IReducersRegistrar _reducersRegistrar;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootStateProviders"/> class.
    /// </summary>
    /// <param name="reducersRegistrar">All the reducers in the system.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> to possibly get state from.</param>
    public AggregateRootStateProviders(
        IReducersRegistrar reducersRegistrar,
        IImmediateProjections immediateProjections)
    {
        _reducersRegistrar = reducersRegistrar;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public Task<IAggregateRootStateProvider> CreateFor(AggregateRoot aggregateRoot)
    {
        if (!aggregateRoot.IsStateful)
        {
            return Task.FromResult<IAggregateRootStateProvider>(NullAggregateRootStateProvider.Instance);
        }

        var hasReducer = _reducersRegistrar.HasReducerFor(aggregateRoot.StateType);
        var hasProjection = _immediateProjections.HasProjectionFor(aggregateRoot.StateType);

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
            var reducer = _reducersRegistrar.GetForModelType(aggregateRoot.StateType);
            return Task.FromResult<IAggregateRootStateProvider>(new ReducerAggregateRootStateProvider(aggregateRoot, reducer));
        }

        return Task.FromResult<IAggregateRootStateProvider>(new ProjectionAggregateRootStateProvider(aggregateRoot, _immediateProjections));
    }
}
