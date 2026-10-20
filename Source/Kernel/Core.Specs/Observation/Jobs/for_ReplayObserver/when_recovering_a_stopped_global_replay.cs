// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_recovering_a_stopped_global_replay : given.a_recovered_global_replay
{
    bool _resumed;
    bool _resumedAgain;
    int _failureRecords;

    async Task Because()
    {
        IJob job = _job;
        IReplayObserver replay = _job;
        _resumed = (await job.Resume()).IsSuccess;
        _failureRecords = _stateStorage.State.StatusChanges.Count;
        _resumedAgain = (await replay.Resume()).IsSuccess;
    }

    [Fact] void should_refuse_recovery_through_the_job_interface() => _resumed.ShouldBeFalse();
    [Fact] void should_refuse_repeated_resume() => _resumedAgain.ShouldBeFalse();
    [Fact] void should_leave_a_terminal_failure() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_the_scope_refusal() => _stateStorage.State.StatusChanges[^1].ExceptionMessages.ShouldContain(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_repeat_the_failure_transition() => _stateStorage.State.StatusChanges.Count.ShouldEqual(_failureRecords);
    [Fact] void should_not_contact_silos() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resume_or_prepare_steps() => _jobStepStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
