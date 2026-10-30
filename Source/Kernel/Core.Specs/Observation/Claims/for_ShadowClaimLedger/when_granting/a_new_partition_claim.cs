// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_granting;

public class a_new_partition_claim : given.a_ledger
{
    void Because() => _divergences = _ledger.Grant(_partition, ClaimHolderKind.PartitionReplay, null, _resume);

    [Fact] void should_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(1UL);
    [Fact] void should_allocate_epoch_from_revision() => Claim().Epoch.Value.ShouldEqual(1UL);
    [Fact] void should_store_partition() => Claim().Partition.ShouldEqual(_partition);
    [Fact] void should_store_kind() => Claim().Holder.ShouldEqual(ClaimHolderKind.PartitionReplay);
    [Fact] void should_store_resume_position() => Claim().ResumePosition.ShouldEqual(_resume);
    [Fact] void should_have_no_pending_work() => Claim().PendingWork.ShouldBeEmpty();
    [Fact] void should_have_no_divergence() => _divergences.ShouldBeEmpty();
}
