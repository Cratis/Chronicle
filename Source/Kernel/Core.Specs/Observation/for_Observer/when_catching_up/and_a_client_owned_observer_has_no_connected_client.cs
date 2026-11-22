// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

/// <summary>
/// A catch-up job for an observer whose client is gone can only fail, and costs the kernel work doing so. The observer
/// stays behind and catches up when its client subscribes again.
/// </summary>
public class and_a_client_owned_observer_has_no_connected_client : given.an_observer_with_subscription
{
    bool _isPreparingCatchupAfter;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Owner = ObserverOwner.Client };
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
    }

    async Task Because()
    {
        await _observer.CatchUp();
        _isPreparingCatchupAfter = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_not_start_a_catch_up_job() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_not_be_left_preparing_catch_up() => _isPreparingCatchupAfter.ShouldBeFalse();
}
