// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_to_a_stream : given.an_event_seeding_grain
{
    EventToAppend _appended;

    async Task Because()
    {
        await _grain.Seed([new SeedingEntry("source", "type", "{}", [], "Order", "Lines", "line-1")]);
        _appended = ((IEnumerable<EventToAppend>)_eventSequence.ReceivedCalls().Single().GetArguments()[0]).Single();
    }

    [Fact] void should_append_with_source_type() => _appended.EventSourceType.Value.ShouldEqual("Order");
    [Fact] void should_append_with_stream_type() => _appended.eventStreamType.Value.ShouldEqual("Lines");
    [Fact] void should_append_with_stream_id() => _appended.eventStreamId.Value.ShouldEqual("line-1");
    [Fact] void should_track_stream_id() => TrackedByEventType.Single().EventStreamId.Value.ShouldEqual("line-1");
}
