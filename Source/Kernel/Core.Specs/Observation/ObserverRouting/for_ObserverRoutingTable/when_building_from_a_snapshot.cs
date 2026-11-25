// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRoutingTable;

public class when_building_from_a_snapshot : given.a_routing_table
{
    ObserverRoute _route;

    void Because() => _table.TryGetRoute(_secondObserver, out _route);

    [Fact] void should_carry_the_revision_of_the_snapshot() => _table.Revision.ShouldEqual(7L);
    [Fact] void should_carry_the_revision_of_each_definition() => _route.Revision.ShouldEqual(new ObserverDefinitionRevision(2));
    [Fact] void should_hold_a_route_for_every_definition() => _table.Routes.Count().ShouldEqual(4);
}
