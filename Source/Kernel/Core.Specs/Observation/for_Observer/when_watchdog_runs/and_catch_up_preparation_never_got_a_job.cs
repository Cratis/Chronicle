// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Asking for a catch-up job can legitimately end without a job that will register catching-up partitions - the start
/// fails, another node already owns the job, or a stopped job is merely resumed. The observer is left believing it is
/// preparing catch-up, which makes it drop every live event and skip its missed-events check, and being kept alive it
/// is never reactivated out of that. The watchdog must clear the preparation and re-route the observer.
/// </summary>
public class and_catch_up_preparation_never_got_a_job : given.an_observer_with_client_owned_subscription
{
    bool _wasPreparingCatchupAfterFailedStart;
    bool _isPreparingCatchupAfterWatchdog;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.CatchUp();
        _wasPreparingCatchupAfterFailedStart = await _observer.IsPreparingCatchup();
        await _observer.RunWatchdogAsync();
        _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_be_left_preparing_catch_up_when_no_job_took_ownership() => _wasPreparingCatchupAfterFailedStart.ShouldBeTrue();
    [Fact] void should_clear_the_stranded_catch_up_preparation() => _isPreparingCatchupAfterWatchdog.ShouldBeFalse();
    [Fact] void should_leave_the_observer_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_have_asked_for_a_catch_up_job() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_re_route_the_observer_back_onto_its_queue() => _appendedEventsQueues
        .Received(1)
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
