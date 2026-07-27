// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// The watchdog's checks all recover by re-routing, and routing starts a catch-up. When that catch-up cannot get a
/// job the observer is left preparing catch-up - which is exactly what the stranded-preparation check looks for. A
/// tick that kept going would take the aftermath of its own recovery as a second fault and recover it again, running
/// two catch-up attempts and two queue unsubscribe/subscribe cycles for one problem.
/// </summary>
public class and_a_missing_catchup_job_leaves_the_observer_preparing_catchup : given.an_observer_behind_on_a_relevant_event
{
    void Establish() => _stateStorage.State.CatchingUpPartitions.Add(new Key("some-partition", ArrayIndexers.NoIndexers));

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_attempt_the_catch_up_once() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_re_subscribe_the_observer_to_its_queue_once() => _appendedEventsQueues
        .Received(1)
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());

    [Fact] void should_leave_the_observer_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
