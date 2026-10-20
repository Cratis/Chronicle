// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_starting_a_global_replay : given.a_recovered_global_replay
{
    bool _started;

    void Establish()
    {
        _stateStorage.State.Status = JobStatus.None;
        _job.PrepareSynchronouslyForTesting = true;
    }

    async Task Because()
    {
        _started = (await _job.Start(_request)).IsSuccess;
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse_preparation() => _started.ShouldBeFalse();
    [Fact] void should_fail_as_an_unrecoverable_preparation_error() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_the_scope_refusal() => _stateStorage.State.StatusChanges[^1].ExceptionMessages.ShouldContain(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_start_steps() => _jobStepStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_contact_silos() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
    [Fact] void should_not_claim_a_successful_replay() => _observer.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IObserver.ReleaseRefusedReplay));
}
