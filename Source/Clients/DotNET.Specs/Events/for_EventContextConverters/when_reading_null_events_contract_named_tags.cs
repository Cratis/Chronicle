// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_reading_null_events_contract_named_tags : Specification
{
    EventContext _context;

    void Because() => _context = new Contracts.Events.EventContext
    {
        EventType = new() { Id = "event", Generation = 1 },
        EventSourceId = "source",
        EventStore = "store",
        Namespace = "namespace",
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        EventSourceType = EventSourceType.Default,
        Causation = [],
        CausedBy = new(),
        Tags = [],
        NamedTags = null!
    }.ToClient();

    [Fact] void should_treat_absent_server_tags_as_empty() => _context.NamedTags.ShouldBeEmpty();
}
