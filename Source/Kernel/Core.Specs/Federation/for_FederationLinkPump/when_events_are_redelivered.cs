// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_events_are_redelivered : given.a_link_pump
{
    void Establish()
    {
        _pull.Enqueue(given.FakePull.Batch("remote", 44, 44, given.FakePull.Event(42), given.FakePull.Event(43)));

        // The producer redelivers 43 together with the new 44.
        _pull.Enqueue(given.FakePull.Batch("remote", 45, 44, given.FakePull.Event(43), given.FakePull.Event(44)));
    }

    async Task Because()
    {
        await _pump.Step(default);
        await _pump.Step(default);
    }

    [Fact] void should_append_every_event_exactly_once() => _inbox.Appended.Select(_ => _.SequenceNumber).ShouldEqual([42UL, 43UL, 44UL]);
    [Fact] void should_record_the_redelivered_event_as_skipped() => _metrics.Received(1).EventSkipped(BindingId, FederationSkipReasons.Duplicate);
}
