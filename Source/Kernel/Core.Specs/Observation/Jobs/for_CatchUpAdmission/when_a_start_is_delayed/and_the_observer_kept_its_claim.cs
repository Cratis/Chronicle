// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpAdmission.when_a_start_is_delayed;

/// <summary>
/// The control for the delayed start: when the claim the start was granted is still the persisted one, it runs.
/// </summary>
public class and_the_observer_kept_its_claim : given.an_admitting_catch_up_job
{
    Result<StartJobError> _result;

    void Establish() => HoldTheObserverStateRead();

    async Task Because()
    {
        var starting = _job.Start(RequestWithEpoch(1));
        _observerStateRead!.SetResult(ClaimedByCatchUp(1));
        _result = await starting;
    }

    [Fact] void should_start() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_job() => _stateStorage.State.Request.ShouldNotBeNull();
}
