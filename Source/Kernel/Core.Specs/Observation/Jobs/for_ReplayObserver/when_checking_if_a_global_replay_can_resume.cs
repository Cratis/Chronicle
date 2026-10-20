// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_checking_if_a_global_replay_can_resume : given.a_recovered_global_replay
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_job.CanResumeForTesting);

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_the_reason() => _stateStorage.State.StatusChanges[^1].ExceptionMessages.ShouldContain(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_contact_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_dispatch_to_silos() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
}
