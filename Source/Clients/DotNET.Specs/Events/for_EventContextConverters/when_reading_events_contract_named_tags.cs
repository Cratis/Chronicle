// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_reading_events_contract_named_tags : Specification
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
        NamedTags = [new() { Name = "name", Value = "" }]
    }.ToClient();

    [Fact] void should_preserve_exact_names_and_values() => _context.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("name", "")]);
}
