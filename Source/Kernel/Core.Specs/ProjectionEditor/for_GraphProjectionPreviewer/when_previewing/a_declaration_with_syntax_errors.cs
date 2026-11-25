// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_declaration_with_syntax_errors : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request(Declaration.Replace("personId Uuid key", "personId Uuid")));

    [Fact] void should_report_the_syntax_errors() => _preview.SyntaxErrors.ShouldNotBeEmpty();
    [Fact] void should_not_describe_a_graph() => _preview.Graph.ShouldBeNull();
    [Fact] void should_not_give_nodes() => _preview.Nodes.ShouldBeEmpty();
    [Fact] void should_not_read_any_event() => _requestedEventTypes.ShouldBeNull();
}
