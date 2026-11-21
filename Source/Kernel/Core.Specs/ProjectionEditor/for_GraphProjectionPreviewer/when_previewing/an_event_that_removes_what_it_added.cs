// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class an_event_that_removes_what_it_added : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    void Establish() => _events.Add(Event(8, Ada, new given.MemberRemoved(Ada, Engineering)));

    async Task Because() => _preview = await _previewer.Preview(Request());

    [Fact] void should_remove_the_edge() => _preview.Edges.Any(_ => _.Type == "MEMBER_OF" && _.SourceKey == Ada.ToString()).ShouldBeFalse();
    [Fact] void should_keep_the_nodes_at_both_ends() => _preview.Nodes.Count().ShouldEqual(4);
}
