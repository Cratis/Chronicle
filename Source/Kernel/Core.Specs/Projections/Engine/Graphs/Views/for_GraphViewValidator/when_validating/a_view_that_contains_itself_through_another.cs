// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_view_that_contains_itself_through_another : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("A", "Group", [new("parent", GraphViewMemberKind.Relation, Edge: "PART_OF", Direction: GraphViewDirection.Outgoing, Cardinality: GraphViewCardinality.One, View: "B")]), new("B", "Group", [new("parent", GraphViewMemberKind.Relation, Edge: "PART_OF", Direction: GraphViewDirection.Outgoing, Cardinality: GraphViewCardinality.One, View: "A")])]);

    [Fact] void should_report_recursiveview() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.RecursiveView);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.RecursiveView).View.ShouldEqual("A");
    [Fact] void should_not_name_a_member() => _issues.Where(_ => _.Reason == GraphViewReason.RecursiveView).All(_ => _.Member is null).ShouldBeTrue();
}
