// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_all_buckets_completed_including_an_empty_one : given.a_bucket_replay
{
    protected override int BucketCount => 3;

    async Task Because()
    {
        await _job.Start(_request);
        await _job.PrepareStepsForTesting(_request);
        var steps = _stateStorage.State.ReplayBucketSteps;
        await _job.RecordStepForTesting(steps[1].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(100UL)));
        await _job.RecordStepForTesting(steps[2].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)));
        await _job.RecordStepForTesting(steps[0].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(10UL)));
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_report_the_global_maximum_when_every_bucket_finished() => _observer.Received(1).ReplayedSuccessfullySince((EventSequenceNumber)100UL, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
    [Fact] void should_count_the_empty_bucket_as_finished() => _stateStorage.State.ReplayBucketSteps[2].Completed.ShouldBeTrue();
    [Fact] void should_not_report_partial_completion() => _observer.DidNotReceive().Replayed(Arg.Any<EventSequenceNumber>());
}
