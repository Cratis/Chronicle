// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ProjectionAggregateRootStateProvider.given;

public class a_projection_aggregate_root_state_provider : Specification
{
    protected ProjectionAggregateRootStateProvider<StateForAggregateRoot> _provider;
    protected IProjections _projections;
    protected IEventSequence _eventSequence;
    protected IAggregateRootEventHandlers _eventHandlers;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();
        _projections = Substitute.For<IProjections>();
    }
}
