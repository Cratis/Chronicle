// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_quarantined_observer_has_no_running_catchup_job : for_Observer.given.a_quarantined_observer
{
    void Establish() => _stateStorage.State = _stateStorage.State with
    {
        NextEventSequenceNumber = 43UL,
        CatchingUpPartitions = new HashSet<Key>(["partition"])
    };

    async Task Because() => await RunWatchdogTicks();

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_catching_up_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"partition");
    [Fact] void should_keep_the_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_start_catchup() => ShouldNotHaveStartedCatchup();
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
