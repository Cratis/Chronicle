// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpAdmission.when_resuming;

/// <summary>
/// The control for the superseded resume: a stopped job whose epoch is still the persisted claim is admitted. What the
/// resume does next is the job's own business, so the spec only says the admission did not stop it.
/// </summary>
public class and_the_claim_is_still_held : given.an_admitting_catch_up_job
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        PersistStoppedJob(RequestWithEpoch(1));
        _persistedObserverState = ClaimedByCatchUp(1);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_not_be_refused_admission() => _result.WasNotAdmitted().ShouldBeFalse();
    [Fact] async Task should_have_read_the_claim() => await _namespaceStorage.Observers.Received(1).Get(_request.ObserverKey.ObserverId);
}
