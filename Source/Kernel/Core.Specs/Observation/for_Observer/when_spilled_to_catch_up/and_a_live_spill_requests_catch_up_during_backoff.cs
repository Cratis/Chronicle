// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_spilled_to_catch_up;

public class and_a_live_spill_requests_catch_up_during_backoff : given.an_observer_settling_after_zero_work
{
    async Task Establish()
    {
        await _observer.CaughtUp(_jobId, 0UL);
        (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
        _queued = false;
        _jobsManager.ClearReceivedCalls();
    }

    Task Because() => _observer.CatchUp();

    [Fact] async Task should_acquire_a_catch_up_job() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] async Task should_prepare_catch_up() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
}
