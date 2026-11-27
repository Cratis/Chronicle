// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_p_is_permanently_failed_on_the_real_queue : given.an_observer_joining_a_real_queue
{
    Task Because() => JoinAndProbe();

    [Fact] void should_keep_the_failed_episode() => _failedPartitionsStorage.State.IsFailed("P").ShouldBeTrue();
    [Fact] void should_not_pin_the_global_floor_to_failed_p() => (_writes.PersistedState.NextEventSequenceNumber > 11UL).ShouldBeTrue();
    [Fact] void should_have_a_live_lane() => LaneWasReplaced.ShouldBeTrue();
    [Fact] void should_deliver_healthy_backlog() => _delivered.Contains((EventSequenceNumber)12UL).ShouldBeTrue();
    [Fact] void should_deliver_the_later_live_probe() => _delivered.Contains((EventSequenceNumber)(SpillDuringJoin ? 17UL : 14UL)).ShouldBeTrue();
    [Fact] void should_not_restart_catch_up_on_a_watchdog_turn() => WatchdogStartedAnotherJob.ShouldBeFalse();
}
