// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

/// <summary>
/// A step that did not complete may have left events behind the furthest point another step read, so the observer
/// is only told what was handled.
/// </summary>
public class and_a_step_did_not_complete : given.a_catch_up_observer_job
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
        _stateStorage.State.Progress.SuccessfulSteps = 1;
        _stateStorage.State.Progress.FailedSteps = 1;
        await _job.CompleteForTesting();
    }

    [Fact] async Task should_not_let_the_observer_move_past_what_was_handled() =>
        await _observer.Received(1).CaughtUp((EventSequenceNumber)3UL, EventSequenceNumber.Unavailable);
}
