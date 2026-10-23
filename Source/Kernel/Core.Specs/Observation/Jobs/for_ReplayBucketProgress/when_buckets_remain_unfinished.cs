// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_buckets_remain_unfinished : Specification
{
    IList<ReplayBucketStep> _steps;
    readonly JobStepId _partial = Guid.NewGuid();
    EventSequenceNumber _position;

    void Establish() => _steps =
    [
        new(Guid.NewGuid(), new(0, 4, 1), 100UL, true),
        new(_partial, new(1, 4, 1), EventSequenceNumber.Unavailable),
        new(Guid.NewGuid(), new(2, 4, 1), 20UL),
        new(Guid.NewGuid(), new(3, 4, 1), EventSequenceNumber.Unavailable, true)
    ];

    void Because()
    {
        ReplayBucketProgress.Record(_steps, _partial, JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(10UL), ["failed"], "")), new JsonSerializerOptions());
        _position = ReplayBucketProgress.Position(_steps);
    }

    [Fact] void should_take_the_minimum_durable_unfinished_position() => _position.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_not_count_a_partial_result_as_completion() => _steps.Single(step => step.Id == _partial).Completed.ShouldBeFalse();
    [Fact] void should_exclude_a_completed_empty_bucket() => _steps.Last().Completed.ShouldBeTrue();
}
