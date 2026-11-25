// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class an_invalid_view : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    async Task Because() => _preview = await _previewer.Preview(Request() with
    {
        Views = [new("People", "Person", [new("friends", "relation", Edge: "FRIEND_OF", View: "Friends")]), new("Friends", "Person", [])]
    });

    [Fact] void should_report_the_rule_that_is_broken() => _preview.ViewErrors.Single().Reason.ShouldEqual("UnknownEdge");
    [Fact] void should_name_the_view() => _preview.ViewErrors.Single().View.ShouldEqual("People");
    [Fact] void should_not_read_a_view() => _preview.Rows.ShouldBeEmpty();
    [Fact] void should_still_give_the_graph() => _preview.Nodes.Count().ShouldEqual(4);
}
