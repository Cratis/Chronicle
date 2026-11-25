// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_declaration_that_is_not_a_graph_projection : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request("projection Anything => SomeReadModel\n  from PersonRegistered\n"));

    [Fact] void should_say_a_graph_is_needed() => _preview.SyntaxErrors.Single().Message.ShouldContain("graph");
    [Fact] void should_not_give_nodes() => _preview.Nodes.ShouldBeEmpty();
}
