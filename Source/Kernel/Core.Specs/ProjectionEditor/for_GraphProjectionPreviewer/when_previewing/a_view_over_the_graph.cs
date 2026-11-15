// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class a_view_over_the_graph : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request() with
    {
        Views =
        [
            new("PersonWithGroups", "Person", [new("id", "property", "personId"), new("name", "property", "name"), new("groups", "relation", Edge: "MEMBER_OF", Cardinality: "many", View: "Membership")]),
            new("Membership", "Group", [new("id", "property", "groupId"), new("name", "property", "name"), new("since", "edgeProperty", "since")])
        ]
    });

    [Fact] void should_have_no_view_errors() => _preview.ViewErrors.ShouldBeEmpty();
    [Fact] void should_give_a_row_for_every_person_in_key_order() => _preview.Rows.Select(_ => _["name"]!.GetValue<string>()).ShouldEqual(["Ada", "Grace"]);
    [Fact] void should_nest_the_related_groups() => _preview.Rows.First()["groups"]!.AsArray().Single()!["name"]!.GetValue<string>().ShouldEqual("Engineering");
    [Fact] void should_read_the_properties_of_the_edge_that_was_followed() => _preview.Rows.First()["groups"]!.AsArray().Single()!["since"]!.GetValue<string>().StartsWith("2026-01-06", StringComparison.Ordinal).ShouldBeTrue();
}
