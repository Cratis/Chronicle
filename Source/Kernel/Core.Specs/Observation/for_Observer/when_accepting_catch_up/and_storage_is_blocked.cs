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
    ICatchUpAdmission _admission;
    bool _admitted;

    protected override ICatchUpAdmission CreateCatchUpAdmission() => _admission = base.CreateCatchUpAdmission();

    protected virtual Task Acknowledge() => _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);

    async Task Because()
    {
        _writes.BeforeWrite = _write.Invoke;
        var acknowledging = Acknowledge();
        Task? acquiring = null;
        try
        {
            await _write.WaitForStart();
            _jobsManager.ClearReceivedCalls();
            _jobStorage.ClearReceivedCalls();
            _admission.ClearReceivedCalls();
            _acknowledgementWaited = !acknowledging.IsCompleted;
            acquiring = _observer.CatchUp();
            _acquisitionWaited = !acquiring.IsCompleted;
            _lookedUpJobs = _jobsManager.ReceivedCalls().Any() || _jobStorage.ReceivedCalls().Any();
            _admitted = _admission.ReceivedCalls().Any();
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
    [Fact] void should_not_admit_catch_up_while_acknowledging() => _admitted.ShouldBeFalse();
    [Fact] void should_commit_the_scanned_cursor() => (_accepted.NextEventSequenceNumber <= 10UL).ShouldBeTrue();
}
