// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_events_originated_in_this_event_store : given.a_link_pump
{
    void Establish()
    {
        _pull.Enqueue(given.FakePull.Batch(LocalEventStore, 12, 11, given.FakePull.Event(10), given.FakePull.Event(11)));
        _pull.Enqueue(given.FakePull.Batch("remote", 12, 11));
    }

    async Task Because()
    {
        await _pump.Step(default);
        await _pump.Step(default);
    }

    [Fact] void should_not_append_anything_to_the_inbox() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_record_each_event_as_skipped_for_own_origin() => _metrics.Received(2).EventSkipped(BindingId, FederationSkipReasons.OwnOrigin);
    [Fact] void should_advance_the_cursor_past_the_rejected_events() => _pull.Requests[1].FromSequenceNumber.ShouldEqual(12UL);
}
