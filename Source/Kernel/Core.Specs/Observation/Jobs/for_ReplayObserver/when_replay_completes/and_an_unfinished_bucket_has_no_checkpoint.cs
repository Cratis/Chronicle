// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_an_unfinished_bucket_has_no_checkpoint : given.a_bucket_replay
{
    protected override int BucketCount => 2;

    async Task Because()
    {
        await _job.Start(_request);
        await _job.PrepareStepsForTesting(_request);
        await _job.RecordStepForTesting(_stateStorage.State.ReplayBucketSteps[1].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(100UL)));
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_not_infer_a_checkpoint_from_a_completed_bucket() => _observer.Received(1).Replayed(EventSequenceNumber.Unavailable);
    [Fact] void should_leave_the_missing_checkpoint_unavailable() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
