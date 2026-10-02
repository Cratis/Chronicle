// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_all_events_replay_has_no_running_job : for_Observer.given.an_observer
{
    async Task Establish()
    {
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero);
        _jobsManager.ClearReceivedCalls();
    }

    Task Because() => _observer.RunWatchdogAsync();

    [Fact] void should_start_the_missing_replay_job() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => !_.EventTypes.Any()));
    [Fact] async Task should_be_replaying() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
}
