// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

/// <summary>
/// Every state write reconciles the ledger. A partition whose catch-up job is running stays in the catching-up set, and
/// reconciling must leave its claim exactly as granted: a new epoch would make the admission refuse the running job
/// the next time it resumes or advances a stage (design risk R2).
/// </summary>
public class with_a_partition_still_held_while_its_job_runs : given.a_ledger
{
    PartitionClaim _granted;

    void Establish()
    {
        _ledger.Grant(_partition, ClaimHolderKind.PartitionCatchUp, null, _resume);
        _granted = Claim();
        _catchingUp.Add(_partition);
    }

    void Because()
    {
        _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);
        _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);
    }

    [Fact] void should_keep_the_epoch_the_job_was_started_under() => Claim().Epoch.ShouldEqual(_granted.Epoch);
    [Fact] void should_keep_the_holder() => Claim().Holder.ShouldEqual(ClaimHolderKind.PartitionCatchUp);
    [Fact] void should_not_report_a_release() => _divergences.ShouldBeEmpty();
}
