// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_direction_that_contradicts_the_edge : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("People", "Person", [new("groups", GraphViewMemberKind.Relation, Edge: "MEMBER_OF", Direction: GraphViewDirection.Incoming, Cardinality: GraphViewCardinality.Many, View: "Groups")]), new("Groups", "Group", [])]);

    [Fact] void should_report_edgenotincidentonroot() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.EdgeNotIncidentOnRoot);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.EdgeNotIncidentOnRoot).View.ShouldEqual("People");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.EdgeNotIncidentOnRoot).Member.ShouldEqual("groups");
}
