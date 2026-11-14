// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_querying_views;

public class a_view_through_the_typed_graph : given.a_seeded_organization
{
    static readonly Guid _engineering = Guid.Parse("00000000-0000-0000-0000-000000000101");

    IReadOnlyList<PersonWithGroups> _members;
    PersonRoles? _roles;
    GroupWithParent? _platform;

    async Task Because()
    {
        _members = await _scenario.Graph.Nodes<PersonWithGroups>()
            .WhereRelated<GroupMembership>(_ => _.Groups, groups => groups.Where(_ => _.Id, new GroupId(_engineering)))
            .ToList();
        _roles = await _scenario.Graph.Nodes<PersonRoles>().Where(_ => _.Id, new PersonId(Guid.Parse("00000000-0000-0000-0000-000000000001"))).FirstOrDefault();
        _platform = await _scenario.Graph.Nodes<GroupWithParent>().Where(_ => _.Name, new GroupName("Platform")).FirstOrDefault();
    }

    [Fact] void should_select_the_roots_by_the_related_clause() => _members.Single().Name.ShouldEqual(new PersonName("Ada"));
    [Fact] void should_hydrate_every_related_item() => _members[0].Groups.Select(_ => _.Name!.Value).ShouldEqual(["Engineering", "Research"]);
    [Fact] void should_hydrate_an_edge_property() => _members[0].Groups[0].Since.ShouldEqual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    [Fact] void should_give_one_item_per_identified_edge() => _roles!.Roles.Select(_ => _.Role).ShouldEqual(["Lead", "Reviewer"]);
    [Fact] void should_hydrate_a_relation_of_one() => _platform!.Parent!.Name.ShouldEqual(new GroupName("Engineering"));
}
