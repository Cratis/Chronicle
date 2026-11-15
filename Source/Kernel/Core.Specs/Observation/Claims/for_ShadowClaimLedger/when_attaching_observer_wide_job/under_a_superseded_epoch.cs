// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_attaching_observer_wide_job;

public class under_a_superseded_epoch : given.a_ledger
{
    ClaimEpoch _first;

    void Establish()
    {
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, null, _resume);
        _first = _ledger.ObserverWide!.Epoch;
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, null, _resume);
    }

    void Because() => _ledger.AttachObserverWideJob(_first, JobId.New());

    [Fact] void should_not_attach_the_job() => _ledger.ObserverWide!.Job.ShouldBeNull();
}
