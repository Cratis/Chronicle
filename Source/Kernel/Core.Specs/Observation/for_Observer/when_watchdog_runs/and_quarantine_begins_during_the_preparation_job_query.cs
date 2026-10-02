// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_quarantine_begins_during_the_preparation_job_query : for_Observer.given.an_observer_quarantined_during_a_probe
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        await _observer.CatchUp();
        _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>().Returns(_ =>
        {
            _probeEntered.TrySetResult();
            return _query.Task;
        });
    }

    async Task Because() => await QuarantineDuringProbe(_observer.RunWatchdogAsync(), () => _query.SetResult(ImmutableList<JobState>.Empty));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_leave_preparation_alone() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
    [Fact] void should_not_resubscribe() => _appendedEventsQueues.DidNotReceiveWithAnyArgs().Subscribe(default!, default!, default);
}
