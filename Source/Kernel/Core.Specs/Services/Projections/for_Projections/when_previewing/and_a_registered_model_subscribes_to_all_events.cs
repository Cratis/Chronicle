// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_a_registered_model_subscribes_to_all_events : given.a_mixed_all_event_preview
{
    async Task Because() => await _service.Preview(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = "projection PreviewIssues"
    });

    [Fact] void should_preview_every_event_including_the_unmapped_type() => _previewedEvents.Select(@event => @event.Context.EventType).ShouldEqual<IEnumerable<Concepts.Events.EventType>>([Mapped, Unmapped, Mapped]);
}
