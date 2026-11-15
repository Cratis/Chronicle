// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over_observer_wide_by_kind;

public class a_claim_held_by_another_job : given.a_ledger
{
    void Establish() => _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, JobId.New(), _resume);
    void Because() => _ledger.HandOverObserverWide(ClaimHolderKind.ObserverCatchUp, JobId.New());

    [Fact] void should_keep_the_claim() => _ledger.ObserverWide.ShouldNotBeNull();
}
