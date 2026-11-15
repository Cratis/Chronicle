// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_property_member_without_a_source : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("People", "Person", [new("name", GraphViewMemberKind.Property)])]);

    [Fact] void should_report_invalidmember() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.InvalidMember);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.InvalidMember).View.ShouldEqual("People");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.InvalidMember).Member.ShouldEqual("name");
}
