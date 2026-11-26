// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class an_edge_property_view_no_relation_leads_to : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("Joined", "Group", [new("since", GraphViewMemberKind.EdgeProperty, "since")])]);

    [Fact] void should_report_edgepropertyviewnotusedastarget() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.EdgePropertyViewNotUsedAsTarget);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.EdgePropertyViewNotUsedAsTarget).View.ShouldEqual("Joined");
    [Fact] void should_not_name_a_member() => _issues.Where(_ => _.Reason == GraphViewReason.EdgePropertyViewNotUsedAsTarget).All(_ => _.Member is null).ShouldBeTrue();
}
