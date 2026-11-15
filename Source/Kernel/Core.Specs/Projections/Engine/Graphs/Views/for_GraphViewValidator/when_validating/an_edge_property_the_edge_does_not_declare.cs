// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class an_edge_property_the_edge_does_not_declare : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("Memberships", "Person", [new("groups", GraphViewMemberKind.Relation, Edge: "MEMBER_OF", Cardinality: GraphViewCardinality.Many, View: "Joined")]), new("Joined", "Group", [new("when", GraphViewMemberKind.EdgeProperty, "joinedOn")])]);

    [Fact] void should_report_unknownedgeproperty() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.UnknownEdgeProperty);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.UnknownEdgeProperty).View.ShouldEqual("Joined");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.UnknownEdgeProperty).Member.ShouldEqual("when");
}
