// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_spilled_to_catch_up;

public class and_zero_work_settles_after_queue_removal : given.an_observer_settling_after_zero_work
{
    void Establish()
    {
        _queued = false;
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)100UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)1UL);
    }

    Task Because() => _observer.CaughtUp(_jobId, 0UL);

    [Fact] void should_rejoin_the_physical_queue() => _queued.ShouldBeTrue();
    [Fact] async Task should_keep_the_logical_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_clear_backoff_for_the_rejoin() => (await _observer.IsCatchUpBackedOff()).ShouldBeFalse();
    [Fact] async Task should_start_exactly_one_catch_up() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] async Task should_catch_up_from_the_gap_start() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == (EventSequenceNumber)1UL));
}
