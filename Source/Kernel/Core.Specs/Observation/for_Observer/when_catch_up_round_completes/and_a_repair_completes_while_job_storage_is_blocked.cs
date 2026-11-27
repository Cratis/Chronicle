// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_repair_completes_while_job_storage_is_blocked : given.an_observer_behind_a_repair_barrier
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _blockedJobs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_blockedJobs.Task);
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 13UL, succeeded: true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_accept_the_acknowledgement_during_a_non_interleaved_turn() => typeof(IObserver).GetMethod(nameof(IObserver.HandoverRepairCaughtUp))!.GetCustomAttributes(typeof(AlwaysInterleaveAttribute), false).Length.ShouldEqual(1);
    [Fact] void should_acknowledge_without_a_job_lookup() => _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
    [Fact] void should_not_start_a_successor_inside_the_acknowledgement() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_persist_the_removed_obligation() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
}
