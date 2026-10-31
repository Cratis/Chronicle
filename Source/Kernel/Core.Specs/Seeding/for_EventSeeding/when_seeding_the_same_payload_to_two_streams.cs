// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_the_same_payload_to_two_streams : given.an_event_seeding_grain
{
    EventToAppend[] _appended;

    async Task Because()
    {
        await _grain.Seed([
            new SeedingEntry("source", "type", "{}", [], "Order", "Lines", "line-1"),
            new SeedingEntry("source", "type", "{}", [], "Order", "Lines", "line-2")
        ]);
        _appended = ((IEnumerable<EventToAppend>)_eventSequence.ReceivedCalls().Single().GetArguments()[0]).ToArray();
    }

    [Fact] void should_append_both() => _appended.Length.ShouldEqual(2);
    [Fact] void should_keep_the_first_stream() => _appended[0].eventStreamId.Value.ShouldEqual("line-1");
    [Fact] void should_keep_the_second_stream() => _appended[1].eventStreamId.Value.ShouldEqual("line-2");
}
