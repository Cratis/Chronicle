// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class the_nodes_and_edges : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request());

    [Fact] void should_have_no_syntax_errors() => _preview.SyntaxErrors.ShouldBeEmpty();
    [Fact] void should_read_only_the_events_the_projection_reacts_to() => _requestedEventTypes.Select(_ => _.Id.Value).Order().ShouldEqual(["GroupCreated", "GroupNested", "MemberAdded", "MemberRemoved", "PersonRegistered", "PersonRemoved"]);
    [Fact] void should_run_every_event_through_the_projection() => _preview.EventsProcessed.ShouldEqual(7);
    [Fact] void should_have_no_event_errors() => _preview.EventErrors.ShouldBeEmpty();
    [Fact] void should_describe_the_graph() => _preview.Graph.Name.ShouldEqual("Organization");
    [Fact] void should_describe_the_node_labels() => _preview.Graph.NodeTypes.Select(_ => _.Label).ShouldEqual(["Person", "Group"]);
    [Fact] void should_describe_the_edge_types() => _preview.Graph.EdgeTypes.Select(_ => _.Type).ShouldEqual(["MEMBER_OF", "PART_OF"]);
    [Fact] void should_describe_the_edge_ends() => _preview.Graph.EdgeTypes.Single(_ => _.Type == "MEMBER_OF").TargetLabel.ShouldEqual("Group");
    [Fact] void should_have_a_node_for_every_person_and_group() => _preview.Nodes.Count().ShouldEqual(4);
    [Fact] void should_give_the_properties_of_a_node() => _preview.Nodes.Single(_ => _.Label == "Person" && _.Key == Ada.ToString()).Properties["name"]!.GetValue<string>().ShouldEqual("Ada");
    [Fact] void should_have_an_edge_for_every_membership_and_nesting() => _preview.Edges.Count().ShouldEqual(3);
    [Fact] void should_give_the_ends_of_an_edge() => _preview.Edges.Single(_ => _.Type == "MEMBER_OF" && _.SourceKey == Grace.ToString()).TargetKey.ShouldEqual(Platform.ToString());
    [Fact] void should_give_the_properties_of_an_edge() => _preview.Edges.Single(_ => _.Type == "MEMBER_OF" && _.SourceKey == Ada.ToString()).Properties["since"]!.GetValue<string>().StartsWith("2026-01-06", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_read_a_view() => _preview.Rows.ShouldBeEmpty();
}
