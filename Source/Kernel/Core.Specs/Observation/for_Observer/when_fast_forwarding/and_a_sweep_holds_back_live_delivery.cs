// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_fast_forwarding;

/// <summary>
/// A fast-forward lowers the hold a sweep keeps over live delivery, and with it lets go of what the sweep held back.
/// </summary>
public class and_a_sweep_holds_back_live_delivery : given.an_observer_with_a_page_subscriber_catching_up
{
    int _heldBackBeforeFastForward;

    async Task Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
        await _observer.Handle((Key)"first", [EventAt(12)]);
        await _observer.Handle((Key)"second", [EventAt(13)]);
        _heldBackBeforeFastForward = _observer.EventsHeldBackForSweep;
    }

    Task Because() => _observer.FastForward();

    [Fact] void should_have_held_back_live_delivery() => _heldBackBeforeFastForward.ShouldEqual(2);
    [Fact] async Task should_lower_the_hold() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_let_go_of_what_was_held_back() => _observer.EventsHeldBackForSweep.ShouldEqual(0);
}
