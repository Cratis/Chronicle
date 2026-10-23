// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_bucket_failures_are_present_in_ordered_state : given.a_performing_job_step
{
    void Establish() => _performState.BucketFailedPartitions = [(Key)"module"];

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_filter_ordered_events() => _handledBatches.SelectMany(batch => batch.SequenceNumbers).SequenceEqual(new EventSequenceNumber[] { 1UL, 2UL, 3UL }).ShouldBeTrue();
    [Fact] void should_checkpoint_every_ordered_event() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_not_change_the_bucket_failures() => _stateStorage.State.BucketFailedPartitions.SequenceEqual(new Key[] { "module" }).ShouldBeTrue();
}
