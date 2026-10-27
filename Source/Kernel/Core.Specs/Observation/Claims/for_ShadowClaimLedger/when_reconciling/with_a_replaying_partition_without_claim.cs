// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

public class with_a_replaying_partition_without_claim : given.a_ledger
{
    void Establish() => _replaying.Add(_partition);
    void Because() => _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);

    [Fact] void should_derive_replay_claim() => Claim().Holder.ShouldEqual(ClaimHolderKind.PartitionReplay);
    [Fact] void should_use_default_resume_position() => Claim().ResumePosition.ShouldEqual(_resume);
    [Fact] void should_not_report_a_release() => _divergences.ShouldBeEmpty();
}
