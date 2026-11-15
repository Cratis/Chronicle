// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_view_whose_root_is_not_a_node : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("Ghosts", "Ghost", [])]);

    [Fact] void should_report_viewrootnotanode() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.ViewRootNotANode);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.ViewRootNotANode).View.ShouldEqual("Ghosts");
    [Fact] void should_not_name_a_member() => _issues.Where(_ => _.Reason == GraphViewReason.ViewRootNotANode).All(_ => _.Member is null).ShouldBeTrue();
}
