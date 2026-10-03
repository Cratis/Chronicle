// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_explicit_event_types_are_empty_while_in_replay_without_a_job : for_Observer.given.an_observer
{
    ObserverRunningState _runningStateAfterFirstTick;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();

        // A re-subscription while already replaying bypasses Routing's event-type check and remains in Replay.
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();

        // The jobs manager reports no replay job, as if the job started on subscription has since vanished.
        _jobsManager.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        _runningStateAfterFirstTick = _stateStorage.State.RunningState;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_look_for_the_replay_job_once() => _jobsManager.Received(1).GetJobs(Arg.Any<JobQuery>());
    [Fact] void should_route_to_disconnected_on_the_first_tick() => _runningStateAfterFirstTick.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] async Task should_remain_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_remain_out_of_replay() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_keep_the_pending_replay() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_not_start_another_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_route_only_once_across_both_ticks() => _eventSequence.Received(1).GetTailSequenceNumber();
}
