// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootStateProviders.given;

public class aggregate_root_state_providers : Specification
{
    protected AggregateRootStateProviders _stateProviders;
    protected IReducers _reducers;
    protected IProjections _projections;

    protected IAggregateRootContext _aggregateRootContext;
    protected EventSourceId _eventSourceId;

    void Establish()
    {
        _reducers = Substitute.For<IReducers>();
        _projections = Substitute.For<IProjections>();

        _stateProviders = new(
            _reducers,
            _projections);

        _aggregateRootContext = Substitute.For<IAggregateRootContext>();
        _eventSourceId = Guid.NewGuid();
        _aggregateRootContext.EventSourceId.Returns(_eventSourceId);
    }
}
