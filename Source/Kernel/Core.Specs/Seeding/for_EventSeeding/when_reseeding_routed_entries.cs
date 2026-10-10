// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_reseeding_routed_entries : given.an_event_seeding_grain
{
    SeedingEntry[] _entries;

    async Task Establish()
    {
        _entries = [new SeedingEntry("source", "type", "{}", [], "Order", "Lines", "line-1")];
        await _grain.Seed(_entries);
        _eventSequence.ClearReceivedCalls();
        _state.ClearReceivedCalls();
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact] void should_not_append_again() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_one_tracked_entry() => TrackedByEventType.Count().ShouldEqual(1);
    [Fact] void should_not_write_tracking_again() => _state.DidNotReceive().WriteStateAsync();
}
