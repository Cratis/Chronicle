// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewValidator.when_validating;

public class a_self_edge_without_a_direction : given.an_organization_definition
{
    IReadOnlyList<GraphViewIssue> _issues;

    void Because() => _issues = GraphViewValidator.Validate(_graph, [new("Groups", "Group", [new("parent", GraphViewMemberKind.Relation, Edge: "PART_OF", Cardinality: GraphViewCardinality.One, View: "Groups")])]);

    [Fact] void should_report_ambiguousdirection() => _issues.ShouldContain(_ => _.Reason == GraphViewReason.AmbiguousDirection);
    [Fact] void should_name_the_view() => _issues.First(_ => _.Reason == GraphViewReason.AmbiguousDirection).View.ShouldEqual("Groups");
    [Fact] void should_name_the_member() => _issues.Single(_ => _.Reason == GraphViewReason.AmbiguousDirection).Member.ShouldEqual("parent");
}
