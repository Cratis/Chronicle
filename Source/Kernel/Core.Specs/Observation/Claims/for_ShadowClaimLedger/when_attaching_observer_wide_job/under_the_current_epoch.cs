// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_attaching_observer_wide_job;

public class under_the_current_epoch : given.a_ledger
{
    JobId _job;

    void Establish()
    {
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, null, _resume);
        _job = JobId.New();
    }

    void Because() => _ledger.AttachObserverWideJob(_ledger.ObserverWide!.Epoch, _job);

    [Fact] void should_attach_the_job() => _ledger.ObserverWide!.Job.ShouldEqual(_job);
}
