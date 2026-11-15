// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class two_views_with_the_same_name : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("Same", "Person", []), new("Same", "Group", [])]);

    [Fact] void should_report_duplicateviewname() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.DuplicateViewName);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.DuplicateViewName).View.ShouldEqual("Same");
    [Fact] void should_not_name_a_member() => _issues.Where(_ => _.Reason == GraphViewReason.DuplicateViewName).All(_ => _.Member is null).ShouldBeTrue();
}
