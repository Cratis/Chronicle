// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

public class and_shadow_claim_is_handed_over : given.an_observer_with_metrics
{
    JobId _job;

    async Task Establish()
    {
        _job = new(Guid.NewGuid());
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_job)));
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions(["partition"]);
    }

    async Task Because() => await _observer.CaughtUp(_job, 42, 42);

    [Fact] void should_release_the_observer_wide_claim() => _stateStorage.State.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_the_revision_high_water_mark() => _stateStorage.State.OwnershipRevision.ShouldEqual(new OwnershipRevision(2));
    [Fact] void should_not_measure_normal_hand_over_as_divergence() => _metrics.For("chronicle-observer-claim-divergences").ShouldBeEmpty();
}
