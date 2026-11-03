// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpAdmission.when_resuming;

/// <summary>
/// A stopped catch-up job started under epoch 1 is resumed after the observer granted a newer catch-up claim. It must
/// stay stopped rather than run alongside the job that holds the claim now.
/// </summary>
public class and_the_claim_was_superseded : given.an_admitting_catch_up_job
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        PersistStoppedJob(RequestWithEpoch(1));
        _persistedObserverState = ClaimedByCatchUp(2);
    }

    async Task Because()
    {
        _result = await _job.Resume();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_be_refused_admission() => _result.WasNotAdmitted().ShouldBeTrue();
    [Fact] void should_stay_stopped() => _stateStorage.State.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_any_subscriber() => _catchupServiceClient.ReceivedCalls().ShouldBeEmpty();
}
