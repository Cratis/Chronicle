// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.when_creating_a_plan;

public class with_an_identified_edge : given.a_plan_factory
{
    GraphProjectionPlan _plan;

    void Because() => _plan = _factory.Create("organization", Edges("VISITED", EdgeFrom("Visited+1", "visitId")), Graph);

    [Fact] void should_map_the_edge_with_its_identity() => _plan.GetMappingsFor(new("Visited", 1)).Edges[0].Identity.ShouldNotBeNull();
}
