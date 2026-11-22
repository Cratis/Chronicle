// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

public class and_a_client_owned_observer_has_no_connected_client : given.an_observer_with_subscription
{
    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Owner = ObserverOwner.Client };
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
    }

    async Task Because() => await _observer.CatchUp();

    [Fact] void should_start_the_catch_up_job() => _jobsManager.Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
