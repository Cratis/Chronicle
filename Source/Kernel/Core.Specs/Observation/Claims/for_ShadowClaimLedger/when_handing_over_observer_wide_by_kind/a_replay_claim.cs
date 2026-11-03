// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over_observer_wide_by_kind;

public class a_replay_claim : given.a_ledger
{
    void Establish() => _ledger.GrantObserverWide(ClaimHolderKind.ObserverReplay, null, _resume);
    void Because() => _ledger.HandOverObserverWide(ClaimHolderKind.ObserverCatchUp, JobId.New());

    [Fact] void should_keep_the_replay_claim() => _ledger.ObserverWide!.Holder.ShouldEqual(ClaimHolderKind.ObserverReplay);
}
