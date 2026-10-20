// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_recovering_a_running_dual_scoped_replay : given.a_recovered_global_replay
{
    bool _resumed;
    protected override ProjectionScope Scope => ProjectionScope.Both;

    void Establish() => _stateStorage.State.Status = JobStatus.Running;

    async Task Because()
    {
        IJob job = _job;
        _resumed = (await job.Resume()).IsSuccess;
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse_recovery_even_when_can_resume_would_be_skipped() => _resumed.ShouldBeFalse();
    [Fact] void should_fail_the_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_finalize_or_resume_on_silos() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).Replayed(EventSequenceNumber.Unavailable);
    [Fact] void should_not_visit_steps() => _jobStepStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_claim_a_successful_replay() => _observer.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IObserver.Replayed));
}
