// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_legacy_callback_waits_for_job_finalization : given.an_observer_with_partition_replay_jobs
{
    bool _held;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetReplayJobs(ReplayJob(_firstJob));
        EventSequenceHasNextEvent(48UL);
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, []);
        _held = _stateStorage.State.ReplayingPartitions.Contains(_partition);
        SetReplayJobs();
        await RunRetryTimer();
    }

    [Fact] void should_hold_while_the_legacy_job_is_running() => _held.ShouldBeTrue();
    [Fact] void should_resume_after_finalization_without_another_callback() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_remove_the_legacy_completion_after_handoff() => _stateStorage.State.PartitionReplayCompletions!.ShouldBeEmpty();
}
