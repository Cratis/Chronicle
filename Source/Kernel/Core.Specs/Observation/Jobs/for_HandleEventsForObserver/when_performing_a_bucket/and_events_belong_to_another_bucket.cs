// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_events_belong_to_another_bucket : given.a_performing_job_step
{
    EventSourceId _other;

    void Establish()
    {
        _performState.Bucket = ReplayBuckets.For("module", 8);
        _other = Enumerable.Range(0, 100).Select(index => new EventSourceId($"other-{index}"))
            .First(source => ReplayBuckets.For(source, 8) != _performState.Bucket);
        _eventCursor.Current.Returns([CreateEvent(1UL, _other), CreateEvent(2UL, "module"), CreateEvent(3UL, _other)]);
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_handle_only_its_own_events() => _handledBatches.SelectMany(batch => batch.SequenceNumbers).ShouldEqual(new EventSequenceNumber[] { 2UL });
    [Fact] void should_not_advance_the_durable_checkpoint_over_skipped_events() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
