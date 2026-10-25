// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

public class and_shadow_claim_keeps_job_and_resume_position : given.an_observer_with_metrics
{
    JobId _job;

    void Establish()
    {
        _job = new(Guid.NewGuid());
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 41, NextEventSequenceNumber = 42 };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_job)));
    }

    async Task Because()
    {
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions(["partition"]);
    }

    [Fact] void should_keep_the_known_job() => _stateStorage.State.ObserverWideClaim.Job.ShouldEqual(_job);
    [Fact] void should_use_an_observer_catch_up_claim() => _stateStorage.State.ObserverWideClaim.Holder.ShouldEqual(ClaimHolderKind.ObserverCatchUp);
    [Fact] void should_keep_the_resume_position() => _stateStorage.State.ObserverWideClaim.ResumePosition.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_allocate_an_epoch() => _stateStorage.State.ObserverWideClaim.Epoch.ShouldEqual(new ClaimEpoch(1));
    [Fact] void should_not_measure_normal_intent_as_divergence() => _metrics.For("chronicle-observer-claim-divergences").ShouldBeEmpty();
}
