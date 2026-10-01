// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_explicit_event_types_are_empty_while_replaying : for_Observer.given.an_observer
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _eventSequence.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    async Task Because()
    {
        // Consecutive ticks must leave a deliberately deferred replay alone, not repeatedly route it.
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_remain_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_remain_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_keep_the_pending_replay() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_not_look_for_a_replay_job() => _jobsManager.DidNotReceive().GetJobsOfType<IReplayObserver, ReplayObserverRequest>();
    [Fact] void should_not_start_a_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_not_route_again() => _eventSequence.DidNotReceive().GetTailSequenceNumber();
    [Fact] void should_not_persist_state_again() => _storageStats.Writes.ShouldEqual(0);
}
