// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_granting;

public class while_an_observer_wide_claim_is_held : given.a_ledger
{
    JobId _job;

    void Establish()
    {
        _job = Guid.NewGuid();
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, _job, _resume);
    }

    void Because() => _divergences = _ledger.Grant(_partition, ClaimHolderKind.PartitionReplay, null, _resume);

    [Fact] void should_report_exclusion() => _divergences.ShouldContain((_partition, ShadowClaimDivergence.ExclusionViolated));
    [Fact] void should_still_record_partition_claim() => Claim().Partition.ShouldEqual(_partition);
    [Fact] void should_keep_observer_wide_job() => _ledger.Snapshot().Wide.Job.ShouldEqual(_job);
}
