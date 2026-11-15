// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class an_event_store_without_events : given.a_previewer
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request());

    [Fact] void should_still_describe_the_graph() => _preview.Graph.Name.ShouldEqual("Organization");
    [Fact] void should_have_no_nodes() => _preview.Nodes.ShouldBeEmpty();
    [Fact] void should_have_no_edges() => _preview.Edges.ShouldBeEmpty();
    [Fact] void should_have_processed_no_events() => _preview.EventsProcessed.ShouldEqual(0);
}
