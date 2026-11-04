// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

/// <summary>
/// Design risk R2: a partition leaves the catching-up set while a job started under its claim still exists. Reconciling
/// releases the claim, so that job is refused when it next resumes or advances a stage. The removers of the set either
/// drop the partition's job first (fast-forward, an observer-wide replay), remove it only when no job owns it (routing),
/// or remove it because the job itself reported back or never started; a partition whose start failed with an unknown
/// outcome is reported failed and recovered by a retry, which runs under no claim. Refusing the leftover job is then
/// what keeps it from delivering beside the partition's new owner.
/// </summary>
public class with_a_partition_leaving_the_set_while_its_job_runs : given.a_ledger
{
    ulong _epoch;
    bool _stillAdmitted;

    void Establish()
    {
        _ledger.Grant(_partition, ClaimHolderKind.PartitionCatchUp, null, _resume);
        _epoch = Claim().Epoch.Value;
    }

    void Because()
    {
        _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);
        var (claims, wide, _) = _ledger.Snapshot();
        var request = new CatchUpObserverPartitionRequest(
            new ObserverKey("observer", "event-store", "namespace", "event-sequence"),
            ObserverType.Reactor,
            _partition,
            EventSequenceNumber.First,
            []) { OwnershipEpoch = _epoch };
        _stillAdmitted = !ObserverJobScope.IsObsolete(request, ObserverState.Empty with { PartitionClaims = claims, ObserverWideClaim = wide });
    }

    [Fact] void should_release_the_claim() => _ledger.ClaimFor(_partition).ShouldBeNull();
    [Fact] void should_report_the_release() => _divergences.ShouldContain((_partition, ShadowClaimDivergence.ReleasedWithoutHandOver));
    [Fact] void should_no_longer_admit_the_job_started_under_it() => _stillAdmitted.ShouldBeFalse();
}
