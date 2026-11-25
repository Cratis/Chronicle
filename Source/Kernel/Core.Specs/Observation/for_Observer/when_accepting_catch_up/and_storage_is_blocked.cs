// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_storage_is_blocked : given.an_observer_accepting_a_catch_up_handover
{
    readonly given.held_storage_operation<bool> _write = new();
    bool _acknowledgementWaited;
    bool _acquisitionWaited;
    bool _lookedUpJobs;
    ObserverState _accepted;

    async Task Because()
    {
        _writes.BeforeWrite = _write.Invoke;
        var acknowledging = _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        Task? acquiring = null;
        try
        {
            await _write.WaitForStart();
            _acknowledgementWaited = !acknowledging.IsCompleted;
            acquiring = _observer.CatchUp();
            _acquisitionWaited = !acquiring.IsCompleted;
            _lookedUpJobs = _jobsManager.ReceivedCalls().Any();
        }
        finally
        {
            _write.Complete(true);
        }
        await acknowledging.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _accepted = _writes.PersistedState;
        if (acquiring is not null) await acquiring.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_acknowledge_before_storage() => _acknowledgementWaited.ShouldBeTrue();
    [Fact] void should_keep_the_handover_barrier_until_storage_accepts() => _acquisitionWaited.ShouldBeTrue();
    [Fact] void should_not_acquire_a_job_while_acknowledging() => _lookedUpJobs.ShouldBeFalse();
    [Fact] void should_commit_the_scanned_cursor() => _accepted.NextEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)13UL);
    [Fact] void should_commit_the_missed_partition_with_the_cursor() => _accepted.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
}
