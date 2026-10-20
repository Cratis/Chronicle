// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

/// <summary>
/// A finalization that fails may have lost writes for events the steps handled, so the job fails and the observer is
/// not moved past them.
/// </summary>
public class and_finalizing_the_catch_up_fails : given.a_catch_up_observer_job
{
    readonly Exception _finalizationFailure = new("final flush outcome unknown");
    Exception? _error;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 3UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 7UL;
        _catchupServiceClient.EndCatchupFor(Arg.Any<ObserverDetails>()).Returns(Task.FromException(_finalizationFailure));
    }

    async Task Because()
    {
        await _job.Start(_request);
        _observer.ClearReceivedCalls();
        _error = await Catch.Exception(_job.CompleteForTesting);
    }

    [Fact] void should_fail_with_the_finalization_failure() => _error.ShouldEqual(_finalizationFailure);
    [Fact] async Task should_not_move_the_observer_on() =>
        await _observer.DidNotReceive().CaughtUp(Arg.Any<JobId>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>());
}
