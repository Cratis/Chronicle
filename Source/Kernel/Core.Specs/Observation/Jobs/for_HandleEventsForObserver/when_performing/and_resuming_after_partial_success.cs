// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_resuming_after_partial_success : given.a_performing_job_step
{
    void Establish()
    {
        // A prior run halted after successfully handling event 2. Resuming must continue from the next event
        // in global order, never re-read from the start, so it preserves the parent-before-child ordering.
        _performState.LastSuccessfullyHandledEventSequenceNumber = 2UL;
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_resume_reading_after_the_last_successfully_handled_event() => _startSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
