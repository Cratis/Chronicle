// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

public class with_failure_recovery_claim : given.a_ledger
{
    void Establish() => _ledger.Grant(_partition, ClaimHolderKind.FailureRecovery, null, _resume);
    void Because() => _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);

    [Fact] void should_preserve_intent_only_claim() => Claim().Holder.ShouldEqual(ClaimHolderKind.FailureRecovery);
    [Fact] void should_not_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(1UL);
    [Fact] void should_not_report_a_release() => _divergences.ShouldBeEmpty();
}
