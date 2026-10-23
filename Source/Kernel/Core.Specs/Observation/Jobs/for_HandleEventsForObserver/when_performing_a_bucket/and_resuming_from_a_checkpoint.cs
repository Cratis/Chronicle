// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_resuming_from_a_checkpoint : given.a_performing_job_step
{
    void Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _performState.LastSuccessfullyHandledEventSequenceNumber = 2UL;
        _eventCursor.Current.Returns([CreateEvent(3UL, "module")]);
    }

    async Task Because()
    {
        _performState = ReplaySerialization.Restore(_performState);
        await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_resume_after_the_durable_checkpoint() => _startSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_persisted_bucket() => _performState.Bucket.ShouldEqual(new ReplayBucket(0, 1, 1));
    [Fact] void should_handle_the_remaining_event() => _handledBatches.SelectMany(batch => batch.SequenceNumbers).ShouldEqual(new EventSequenceNumber[] { 3UL });
}
