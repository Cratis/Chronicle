// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

public class and_bucket_assignments_were_persisted : given.a_bucket_replay
{
    ReplayBucketStep[] _original;

    void Establish() => _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });

    async Task Because()
    {
        await _job.Start(_request);
        await _job.PrepareStepsForTesting(_request);
        _original = _stateStorage.State.ReplayBucketSteps.ToArray();
        _stateStorage.State.ReplayBucketSteps = ReplaySerialization.Restore(_original).ToList();
        await _job.ResumeForTesting();
    }

    [Fact] void should_keep_the_persisted_step_ids_and_bucket_hashes() => _stateStorage.State.ReplayBucketSteps.ShouldEqual<ReplayBucketStep>(_original);
    [Fact] void should_keep_the_original_active_step_window() => _job.MaxActiveStepsForTesting.ShouldEqual(8);
    [Fact] void should_not_resolve_a_new_definition_when_resuming() => _eventStoreStorage.Projections.Received(1).Get(Arg.Any<Cratis.Chronicle.Concepts.Projections.ProjectionId>());
}
