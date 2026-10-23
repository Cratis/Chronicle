// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_buckets_finish_out_of_order : Specification
{
    readonly JsonSerializerOptions _options = new();
    readonly JobStepId _first = Guid.NewGuid();
    readonly JobStepId _second = Guid.NewGuid();
    IList<ReplayBucketStep> _steps;
    EventSequenceNumber _before;
    EventSequenceNumber _after;

    void Establish() => _steps = [new(_first, new(0, 2, 1), EventSequenceNumber.Unavailable), new(_second, new(1, 2, 1), EventSequenceNumber.Unavailable)];

    void Because()
    {
        ReplayBucketProgress.Record(_steps, _second, JobStepResult.Succeeded(new HandleEventsForPartitionResult(90UL)), _options);
        _before = ReplayBucketProgress.Position(_steps);
        ReplayBucketProgress.Record(_steps, _first, JobStepResult.Succeeded(new HandleEventsForPartitionResult(10UL)), _options);
        _after = ReplayBucketProgress.Position(_steps);
    }

    [Fact] void should_not_advance_past_a_bucket_with_no_checkpoint() => _before.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_use_the_maximum_only_after_every_bucket_finishes() => _after.ShouldEqual((EventSequenceNumber)90UL);
    [Fact] void should_mark_each_bucket_complete() => _steps.All(step => step.Completed).ShouldBeTrue();
}
