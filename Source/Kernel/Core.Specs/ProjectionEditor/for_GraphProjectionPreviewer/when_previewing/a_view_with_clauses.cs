// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_view_with_clauses : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request() with
    {
        Views =
        [
            new("PersonWithGroups", "Person", [new("name", "property", "name"), new("groups", "relation", Edge: "MEMBER_OF", Cardinality: "many", View: "Group")]),
            new("Group", "Group", [new("name", "property", "name")])
        ],
        View = "PersonWithGroups",
        Clauses = [new(Relation: "groups", Member: "name", Value: "Platform")],
        FirstOnly = true
    });

    [Fact] void should_have_no_view_errors() => _preview.ViewErrors.ShouldBeEmpty();
    [Fact] void should_select_the_roots_that_have_the_related_group() => _preview.Rows.Single()["name"]!.GetValue<string>().ShouldEqual("Grace");
}
