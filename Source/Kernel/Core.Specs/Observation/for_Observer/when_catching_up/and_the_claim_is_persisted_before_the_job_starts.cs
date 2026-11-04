// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

/// <summary>
/// The admission compares a job's epoch with the persisted claim, so the claim has to be written before the job can
/// ask: a start that reaches the admission first would find nothing and be refused.
/// </summary>
public class and_the_claim_is_persisted_before_the_job_starts : given.an_observer_with_subscription
{
    ulong? _requestEpoch;
    ObserverWideClaim? _persistedAtStart;
    int _writesBeforeCatchUp;
    int _writesAtStart;

    void Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(call =>
        {
            _requestEpoch = call.Arg<CatchUpObserverRequest>().OwnershipEpoch;
            _persistedAtStart = _stateStorage.State.ObserverWideClaim;
            _writesAtStart = _storageStats.Writes;
            return Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New()));
        });
    }

    async Task Because()
    {
        _writesBeforeCatchUp = _storageStats.Writes;
        await _observer.CatchUp();
    }

    [Fact] void should_stamp_the_request_with_an_epoch() => _requestEpoch.ShouldNotBeNull();
    [Fact] void should_have_written_the_state_before_starting() => _writesAtStart.ShouldBeGreaterThan(_writesBeforeCatchUp);
    [Fact] void should_have_persisted_a_catch_up_claim_before_starting() => _persistedAtStart!.Holder.ShouldEqual(ClaimHolderKind.ObserverCatchUp);
    [Fact] void should_have_persisted_the_epoch_the_request_carries() => _persistedAtStart!.Epoch.Value.ShouldEqual(_requestEpoch!.Value);
}
