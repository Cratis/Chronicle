// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_querying_views;

public class a_bare_node_an_edge_created : Specification
{
    static readonly Guid _personId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    static readonly Guid _groupId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    GraphScenario _scenario;
    IReadOnlyList<GroupWithMembers> _groups;

    async Task Because()
    {
        _scenario = new GraphScenario();
        await _scenario.Append(_personId, new OrganizationPersonRegistered(_personId, "Ada"));
        await _scenario.Append(_personId, new OrganizationMemberJoined(_personId, _groupId));
        _groups = await _scenario.Graph.Nodes<GroupWithMembers>().ToList();
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_hydrate_the_bare_node() => _groups.Single().Id.ShouldEqual(new GroupId(_groupId));
    [Fact] void should_leave_its_properties_absent() => _groups[0].Name.ShouldBeNull();
    [Fact] void should_follow_the_edge_the_events_projected() => _groups[0].Members.Single().Name.ShouldEqual(new PersonName("Ada"));
}
