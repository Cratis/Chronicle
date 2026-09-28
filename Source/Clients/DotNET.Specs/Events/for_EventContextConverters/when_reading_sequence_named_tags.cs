// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_reading_sequence_named_tags : Specification
{
    EventContext _context;

    void Because() => _context = new Contracts.Sequences.EventContext
    {
        EventType = new() { Id = "event", Generation = 1 },
        EventSourceId = "source",
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        EventSourceType = EventSourceType.Default,
        Causation = [],
        CausedBy = new(),
        Tags = [],
        NamedTags = [new() { Name = "Case", Value = "" }, new() { Name = "case", Value = "same" }]
    }.ToClient("store", "namespace");

    [Fact] void should_preserve_exact_pairs() => _context.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("Case", ""), ("case", "same")]);
}
