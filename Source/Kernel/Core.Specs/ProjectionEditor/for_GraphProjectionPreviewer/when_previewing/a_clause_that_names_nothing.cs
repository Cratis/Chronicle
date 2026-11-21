// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_clause_that_names_nothing : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request() with
    {
        Views = [new("People", "Person", [new("name", "property", "name")])],
        Clauses = [new()]
    });

    [Fact] void should_report_the_query_as_invalid() => _preview.ViewErrors.Single().Reason.ShouldEqual("InvalidQuery");
}
