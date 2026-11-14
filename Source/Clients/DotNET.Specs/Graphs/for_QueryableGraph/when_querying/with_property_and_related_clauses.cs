// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_QueryableGraph.when_querying;

public class with_property_and_related_clauses : given.a_queryable_graph
{
    static readonly Guid _personId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    static readonly Guid _groupId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    IReadOnlyList<PersonWithGroups> _result;

    void Establish() => _executor.Execute(Arg.Any<GraphQuery>(), Arg.Any<CancellationToken>()).Returns(
    [
        new GraphRow(new Dictionary<string, object?>
        {
            ["id"] = _personId.ToString(),
            ["name"] = "Ada",
            ["groups"] = new List<GraphRow>
            {
                new(new Dictionary<string, object?> { ["id"] = _groupId.ToString(), ["name"] = "Engineering", ["since"] = "2026-01-01T00:00:00Z" })
            }
        })
    ]);

    async Task Because() => _result = await _graph.Nodes<PersonWithGroups>()
        .Where(_ => _.Name, "Ada")
        .WhereRelated<GroupMembership>(_ => _.Groups, groups => groups.Where(_ => _.Id, _groupId))
        .ToList();

    [Fact] void should_query_the_graph_of_the_view() => _query!.Graph.ShouldEqual("Organization");
    [Fact] void should_ask_for_a_list() => _query!.Shape.ShouldEqual(GraphQueryShape.List);
    [Fact] void should_query_from_the_root_of_the_view() => (_query!.Root.View, _query.Root.Label, _query.Root.KeyProperty).ShouldEqual(("PersonWithGroups", "Person", "personId"));
    [Fact] void should_add_the_property_clause_with_the_node_property() => _query!.Clauses[0].ShouldEqual(new GraphPropertyClause("name", "name", "Ada"));
    [Fact] void should_add_the_related_clause_on_the_relation() => ((GraphRelatedClause)_query!.Clauses[1]).Member.ShouldEqual("groups");
    [Fact] void should_convert_the_nested_value_to_what_the_graph_stores() => ((GraphRelatedClause)_query!.Clauses[1]).Clauses.Single().ShouldEqual(new GraphPropertyClause("id", "groupId", _groupId.ToString()));
    [Fact] void should_resolve_the_relation_to_its_target_node() => _query!.Root.Members.Single(_ => _.Name == "groups").Relation!.Target.View.ShouldEqual("GroupMembership");
    [Fact] void should_hydrate_the_rows() => _result.Single().Groups.Single().Since.ShouldEqual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
}
