// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.when_previewing;

public class an_event_that_cannot_be_applied : given.an_organization_in_the_event_store
{
    GraphProjectionPreview _preview;

    void Establish() => _events.Add(Event(8, Ada, new given.PersonRemoved(Ada)));

    async Task Because() => _preview = await _previewer.Preview(Request());

    [Fact] void should_report_the_event() => _preview.EventErrors.Single().SequenceNumber.ShouldEqual(8UL);
    [Fact] void should_say_why() => _preview.EventErrors.Single().Message.ShouldNotBeEmpty();
    [Fact] void should_keep_what_the_other_events_did() => _preview.Nodes.Count().ShouldEqual(4);
    [Fact] void should_still_count_the_event_as_processed() => _preview.EventsProcessed.ShouldEqual(8);
}
