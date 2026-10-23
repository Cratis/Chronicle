// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_bucket_stopped_with_a_checkpoint : given.a_bucket_replay
{
    protected override int BucketCount => 2;

    async Task Because()
    {
        await _job.Start(_request);
        await _job.PrepareStepsForTesting(_request);
        var steps = _stateStorage.State.ReplayBucketSteps;
        await _job.RecordStepForTesting(steps[1].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(100UL)));
        await _job.RecordStepForTesting(steps[0].Id, JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(10UL), ["stopped"], "")));
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_resume_live_handling_from_the_unfinished_durable_position() => _observer.Received(1).Replayed((EventSequenceNumber)10UL);
    [Fact] void should_not_report_a_successful_rebuild() => _observer.DidNotReceive().ReplayedSuccessfullySince(Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Cratis.Chronicle.Concepts.Keys.Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
    [Fact] void should_retain_the_unfinished_bucket() => _stateStorage.State.ReplayBucketSteps[0].Completed.ShouldBeFalse();
}
