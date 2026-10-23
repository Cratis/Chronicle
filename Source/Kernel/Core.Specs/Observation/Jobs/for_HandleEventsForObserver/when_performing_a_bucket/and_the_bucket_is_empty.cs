// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_the_bucket_is_empty : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.Bucket = ReplayBuckets.For("module", 8) with { Index = (ReplayBuckets.For("module", 8).Index + 1) % 8 };
        _eventCursor.Current.Returns([CreateEvent(1UL, "module")]);
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_deliver_any_events() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_finish_without_a_handled_checkpoint()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetFullResult<HandleEventsForPartitionResult>(out var handled, out _, new System.Text.Json.JsonSerializerOptions()).ShouldBeTrue();
        handled.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
        handled.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    }
}
