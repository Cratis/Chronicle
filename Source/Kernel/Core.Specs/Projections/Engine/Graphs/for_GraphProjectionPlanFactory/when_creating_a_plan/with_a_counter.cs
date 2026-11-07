// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.when_creating_a_plan;

public class with_a_counter : given.a_plan_factory
{
    GraphProjectionPlan _plan;

    void Because() => _plan = _factory.Create("organization", Nodes("Person", NodeFrom("PersonVisited+1", ("name", "$increment"))), Graph);

    [Fact] void should_need_the_current_state() => _plan.GetMappingsFor(new("PersonVisited", 1)).Nodes[0].ReadsCurrentState.ShouldBeTrue();
}
