// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

/// <summary>
/// The catch-up read events after the last one it handled, all excluded by the observer's filters. The observer is
/// told both, so it can move past the excluded events without counting them as handled.
/// </summary>
public class and_the_steps_read_past_the_last_handled_event : given.a_catch_up_observer_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 3UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 7UL;
    }

    async Task Because()
    {
        await _job.Start(_request);
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.SuccessfulSteps = 2;
        await _job.CompleteForTesting();
    }

    [Fact] async Task should_tell_the_observer_what_was_handled_and_how_far_it_read() =>
        await _observer.Received(1).CaughtUp(_jobId, (EventSequenceNumber)3UL, (EventSequenceNumber)7UL);
}
