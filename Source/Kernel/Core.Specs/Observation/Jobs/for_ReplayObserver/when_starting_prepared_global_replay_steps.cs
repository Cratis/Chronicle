// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_starting_prepared_global_replay_steps : given.a_recovered_global_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(_job.StartPreparedStepsForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_touch_the_sink() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
}
