// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

/// <summary>
/// A handover that cannot persist progress must fail the job's completion, rather than leaving an unobserved error
/// after the job has finalized successfully.
/// </summary>
public class and_the_observers_handover_fails : given.a_catch_up_observer_job
{
    readonly Exception _handoverFailure = new("handover could not persist progress");
    Exception? _error;

    void Establish() => _observer
        .CaughtUp(_jobId, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable)
        .Returns(Task.FromException(_handoverFailure));

    async Task Because()
    {
        await _job.Start(_request);
        _error = await Catch.Exception(_job.CompleteForTesting);
    }

    [Fact] void should_fail_completion_with_the_handover_failure() => _error.ShouldEqual(_handoverFailure);
}
