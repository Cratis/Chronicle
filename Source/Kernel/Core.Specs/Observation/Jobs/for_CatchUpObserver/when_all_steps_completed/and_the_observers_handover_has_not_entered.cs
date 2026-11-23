// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

/// <summary>
/// The callback can wait in transit before the observer registers any conclusion in flight. Finalizing the job in
/// that window leaves no owner and no barrier while the observer still has its old cursor (#6145).
/// </summary>
public class and_the_observers_handover_has_not_entered : given.a_catch_up_observer_job
{
    readonly TaskCompletionSource _handover = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completionWaitedForHandover;

    void Establish() => _observer.CaughtUp(_jobId, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable).Returns(_handover.Task);

    async Task Because()
    {
        await _job.Start(_request);
        var completion = _job.CompleteForTesting();
        _completionWaitedForHandover = !completion.IsCompleted;
        _handover.SetResult();
        await completion;
    }

    [Fact] void should_not_finalize_before_the_observer_acknowledges_its_progress() => _completionWaitedForHandover.ShouldBeTrue();
}
