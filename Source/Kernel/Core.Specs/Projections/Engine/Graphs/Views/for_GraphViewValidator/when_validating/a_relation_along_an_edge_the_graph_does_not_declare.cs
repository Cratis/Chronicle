// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_relation_along_an_edge_the_graph_does_not_declare : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("People", "Person", [new("friends", GraphViewMemberKind.Relation, Edge: "FRIEND_OF", Cardinality: GraphViewCardinality.Many, View: "People")])]);

    [Fact] void should_report_unknownedge() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.UnknownEdge);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.UnknownEdge).View.ShouldEqual("People");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.UnknownEdge).Member.ShouldEqual("friends");
}
