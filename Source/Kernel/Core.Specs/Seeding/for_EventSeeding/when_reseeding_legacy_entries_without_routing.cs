// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_reseeding_legacy_entries_without_routing : given.an_event_seeding_grain
{
    void Establish()
    {
        var legacy = new SeededEventEntry("source", "type", "{}", []);
        _state.State.ByEventType[(EventTypeId)"type"] = [legacy];
        _state.State.ByEventSource[(EventSourceId)"source"] = [legacy];
    }

    async Task Because() => await _grain.Seed([new SeedingEntry("source", "type", "{}", [], new EventSourceType(string.Empty), new EventStreamType(string.Empty), new EventStreamId(string.Empty))]);

    [Fact] void should_not_append_again() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_one_tracked_entry() => TrackedByEventType.Count().ShouldEqual(1);
    [Fact] void should_not_write_tracking_again() => _state.DidNotReceive().WriteStateAsync();
}
