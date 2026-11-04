// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_resuming_a_replay_while_generation_initialization_is_quiescing : given.a_replay_observer_job
{
    Exception _error;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow), new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.LegacyWorkReconciled, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
    }

    async Task Because()
    {
        _error = await Catch.Exception(_job.ResumeForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_not_refuse() => (_error is GenerationInitializationReplayNotSupported).ShouldBeFalse();
    [Fact] void should_not_fail_the_job() => _stateStorage.State.Status.ShouldNotEqual(JobStatus.Failed);
    [Fact] async Task should_not_release_the_observer_as_refused() => await _observer.DidNotReceive().ReleaseRefusedReplay();
}
