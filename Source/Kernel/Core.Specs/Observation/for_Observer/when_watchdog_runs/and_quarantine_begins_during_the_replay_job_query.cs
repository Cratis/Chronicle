// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_quarantine_begins_during_the_replay_job_query : for_Observer.given.an_observer_quarantined_during_a_probe
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, ReplayingPartitions = new HashSet<Key>([_partition]) };
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>().Returns(_ =>
        {
            _probeEntered.TrySetResult();
            return _query.Task;
        });
    }

    async Task Because()
    {
        var watchdog = _observer.RunWatchdogAsync();
        try
        {
            await _probeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.TransitionTo<QuarantinedObserver>();
        }
        finally
        {
            _query.SetResult(ImmutableList<JobState>.Empty);
        }
        await watchdog.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_the_replay_marker() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_start_replay() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_not_start_catchup() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_resubscribe_to_the_queue() => _appendedEventsQueues.DidNotReceive()
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
