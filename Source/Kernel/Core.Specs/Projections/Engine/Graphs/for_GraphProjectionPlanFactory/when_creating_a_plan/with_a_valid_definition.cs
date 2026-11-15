// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.when_creating_a_plan;

public class with_a_valid_definition : given.a_plan_factory
{
    GraphProjectionPlan _plan;

    void Because() => _plan = _factory.Create("organization", Nodes("Person", NodeFrom("PersonRegistered+1", ("name", "name"))), Graph);

    [Fact] void should_react_to_the_event_type() => _plan.EventTypes.Select(_ => _.ToString()).ShouldContain("PersonRegistered+1");
    [Fact] void should_map_the_node() => _plan.GetMappingsFor(new("PersonRegistered", 1)).Nodes.Count.ShouldEqual(1);
    [Fact] void should_not_need_the_current_state_without_counters() => _plan.GetMappingsFor(new("PersonRegistered", 1)).Nodes[0].ReadsCurrentState.ShouldBeFalse();
}
