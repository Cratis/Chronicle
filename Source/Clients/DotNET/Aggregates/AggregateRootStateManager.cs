// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateManager"/>.
/// </summary>
public class AggregateRootStateManager : IAggregateRootStateManager
{
    readonly IReducersRegistrar _reducersRegistrar;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootStateManager"/> class.
    /// </summary>
    /// <param name="reducersRegistrar">All the reducers in the system.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> to possibly get state from.</param>
    public AggregateRootStateManager(
        IReducersRegistrar reducersRegistrar,
        IImmediateProjections immediateProjections)
    {
        _reducersRegistrar = reducersRegistrar;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public async Task Handle(AggregateRoot aggregateRoot, IEnumerable<AppendedEvent> events)
    {
        var reducer = _reducersRegistrar.GetAll().SingleOrDefault(_ => _.ReadModelType == aggregateRoot.StateType);
        if (reducer is not null)
        {
            var reducerResult = await reducer.OnNext(events, null);
            aggregateRoot.SetState(reducerResult.State!);
            return;
        }

        var result = await _immediateProjections.GetInstanceById(aggregateRoot.StateType, aggregateRoot.EventSourceId);
        aggregateRoot.SetState(result.Model);
    }
}
