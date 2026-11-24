// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.when_creating_a_plan;

public class with_a_set_once_mapping : given.a_plan_factory
{
    GraphProjectionPlan _plan;

    void Because() => _plan = _factory.Create("organization", Nodes("Person", NodeFrom("PersonRegistered+1", ("name", "$setOnce(name)"))), Graph);

    [Fact] void should_read_the_existing_value_before_deciding_whether_to_set_it() => _plan.GetMappingsFor(new("PersonRegistered", 1)).Nodes[0].ReadsCurrentState.ShouldBeTrue();
}
