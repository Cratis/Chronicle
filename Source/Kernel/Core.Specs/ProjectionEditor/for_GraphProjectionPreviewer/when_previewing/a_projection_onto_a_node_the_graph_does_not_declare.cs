// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_projection_onto_a_node_the_graph_does_not_declare : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request(Declaration.Replace("projection OrganizationGraph => graph Organization\n  node Person", "projection OrganizationGraph => graph Organization\n  node Stranger")));

    [Fact] void should_report_why_the_projection_is_not_valid() => _preview.SyntaxErrors.ShouldNotBeEmpty();
    [Fact] void should_not_describe_a_graph() => _preview.Graph.ShouldBeNull();
    [Fact] void should_not_read_any_event() => _requestedEventTypes.ShouldBeNull();
}
