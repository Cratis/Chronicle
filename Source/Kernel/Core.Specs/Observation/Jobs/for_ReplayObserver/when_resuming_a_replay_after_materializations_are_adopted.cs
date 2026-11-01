// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_resuming_a_replay_after_materializations_are_adopted : given.a_replay_observer_job
{
    Exception _error;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.MaterializationAdopted, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
    }

    async Task Because()
    {
        _error = await Catch.Exception(_job.ResumeForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationInitializationReplayNotSupported>();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
    [Fact] void should_not_touch_the_sink() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_resume_the_replay_on_silos() => await _replayServiceClient.DidNotReceive().ResumeReplayFor(Arg.Any<ObserverDetails>());
}
