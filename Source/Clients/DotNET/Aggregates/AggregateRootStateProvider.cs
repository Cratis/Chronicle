// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/>.
/// </summary>
public class AggregateRootStateProvider : IAggregateRootStateProvider
{
    readonly IReducersRegistrar _reducersRegistrar;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootStateProvider"/> class.
    /// </summary>
    /// <param name="reducersRegistrar">All the reducers in the system.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> to possibly get state from.</param>
    public AggregateRootStateProvider(
        IReducersRegistrar reducersRegistrar,
        IImmediateProjections immediateProjections)
    {
        _reducersRegistrar = reducersRegistrar;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public async Task Provide(AggregateRoot aggregateRoot, IEnumerable<AppendedEvent> events)
    {
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
            var reducerResult = await reducer.OnNext(events, null);
            aggregateRoot.SetState(reducerResult.State!);
            return;
        }

        var result = await _immediateProjections.GetInstanceById(aggregateRoot.StateType, aggregateRoot._eventSourceId);
        aggregateRoot.SetState(result.Model);
    }
}
