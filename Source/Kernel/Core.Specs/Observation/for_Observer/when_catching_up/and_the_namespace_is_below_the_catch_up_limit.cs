// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

public class and_the_namespace_is_below_the_catch_up_limit : given.an_observer_with_subscription
{
    ICatchUpAdmission _admission;
    IDisposable _lease;

    protected override ICatchUpAdmission CreateCatchUpAdmission()
    {
        _lease = Substitute.For<IDisposable>();
        _admission = Substitute.For<ICatchUpAdmission>();
        _admission.TryAdmit(Arg.Any<ObserverKey>(), Arg.Any<int>()).Returns(Task.FromResult<IDisposable?>(_lease));
        return _admission;
    }

    void Establish() => _jobsManager
        .GetJobs(Arg.Any<JobQuery>())
        .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

    async Task Because() => await _observer.CatchUp();

    [Fact] void should_start_the_catch_up_job() => _jobsManager.Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_release_its_place_once_the_job_is_acquired() => _lease.Received(1).Dispose();
}
