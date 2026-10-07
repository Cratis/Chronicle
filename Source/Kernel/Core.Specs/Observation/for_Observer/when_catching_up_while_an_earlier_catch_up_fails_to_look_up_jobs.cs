// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A catch-up arriving while another is acquiring a job waits for that acquisition and adopts its job. When the
/// acquisition fails it hands over no job, and the waiting catch-up must acquire one itself rather than inherit the
/// failure or be left without an owner (Cratis/Chronicle#4548).
/// </summary>
public class when_catching_up_while_an_earlier_catch_up_fails_to_look_up_jobs : given.an_observer_with_subscription
{
    static readonly JobId _startedJob = JobId.New();

    readonly TaskCompletionSource<IImmutableList<JobState>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _holdNextLookup;
    Exception? _earlierCatchUpError;

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ =>
            {
                if (!_holdNextLookup) return Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty);
                _holdNextLookup = false;
                return _heldLookup.Task;
            });

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_startedJob)));

        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        _holdNextLookup = true;
        var earlierCatchUp = _observer.CatchUp();
        var waitingCatchUp = _observer.CatchUp();
        _heldLookup.SetException(new LookupFailed());
        _earlierCatchUpError = await Cratis.Specifications.Catch.Exception(() => earlierCatchUp);
        await waitingCatchUp;
    }

    [Fact] void should_fail_the_earlier_catch_up() => _earlierCatchUpError.ShouldBeOfExactType<LookupFailed>();

    [Fact]
    async Task should_have_the_waiting_catch_up_start_a_job_itself() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] async Task should_still_be_preparing_catch_up_for_the_started_job() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();

    class LookupFailed : Exception;
}
