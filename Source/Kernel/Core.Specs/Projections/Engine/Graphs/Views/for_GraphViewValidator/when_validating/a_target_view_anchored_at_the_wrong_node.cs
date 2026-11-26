// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_target_view_anchored_at_the_wrong_node : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("People", "Person", [new("groups", GraphViewMemberKind.Relation, Edge: "MEMBER_OF", Cardinality: GraphViewCardinality.Many, View: "Others")]), new("Others", "Person", [])]);

    [Fact] void should_report_targetlabelmismatch() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.TargetLabelMismatch);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.TargetLabelMismatch).View.ShouldEqual("People");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.TargetLabelMismatch).Member.ShouldEqual("groups");
}
