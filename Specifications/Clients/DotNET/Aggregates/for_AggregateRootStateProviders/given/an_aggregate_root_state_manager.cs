// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProviders.given;

public class an_aggregate_root_state_manager : Specification
{
    protected AggregateRootStateProviders state_providers;
    protected Mock<IReducersRegistrar> reducers_registrar;
    protected Mock<IImmediateProjections> immediate_projections;

    protected StatefulAggregateRoot aggregate_root;

    void Establish()
    {
        reducers_registrar = new();
        immediate_projections = new();

        state_providers = new(
            reducers_registrar.Object,
            immediate_projections.Object);

        aggregate_root = new()
        {
            _eventSourceId = Guid.NewGuid().ToString()
        };
    }
}
