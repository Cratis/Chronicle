// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters;

public class when_round_tripping_partition_claims : Specification
{
    KernelObserverState _state;
    KernelObserverState _result;
    KernelObserverState _legacy;
    KernelObserverState _withFailedPartitions;

    void Establish() => _state = KernelObserverState.Empty with
    {
        Identifier = "observer",
        PartitionClaims =
        [
            new("partition", ClaimHolderKind.PartitionCatchUp, null, new(3), 42, []),
            new("other", ClaimHolderKind.PartitionReplay, new(Guid.NewGuid()), new(6), 47, [])
        ],
        ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
        OwnershipRevision = new(7)
    };

    void Because()
    {
        var document = _state.ToMongoDB().ToBsonDocument();
        _result = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _withFailedPartitions = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
        _legacy = BsonSerializer.Deserialize<ObserverState>((KernelObserverState.Empty with { Identifier = "legacy" }).ToMongoDB().ToBsonDocument()).ToKernel();
    }

    [Fact] void should_keep_both_partitions() => _result.PartitionClaims.Select(claim => claim.Partition).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Partition));
    [Fact] void should_keep_the_kinds() => _result.PartitionClaims.Select(claim => claim.Holder).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Holder));
    [Fact] void should_keep_null_and_non_null_jobs() => _result.PartitionClaims.Select(claim => claim.Job).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Job));
    [Fact] void should_keep_epochs() => _result.PartitionClaims.Select(claim => claim.Epoch).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Epoch));
    [Fact] void should_keep_resume_positions() => _result.PartitionClaims.Select(claim => claim.ResumePosition).ShouldEqual(_state.PartitionClaims.Select(claim => claim.ResumePosition));
    [Fact] void should_keep_empty_pending_work() => _result.PartitionClaims.All(claim => claim.PendingWork.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_the_wide_claim_kind() => _result.ObserverWideClaim.Holder.ShouldEqual(_state.ObserverWideClaim.Holder);
    [Fact] void should_keep_the_wide_claim_job() => _result.ObserverWideClaim.Job.ShouldEqual(_state.ObserverWideClaim.Job);
    [Fact] void should_keep_the_wide_claim_epoch() => _result.ObserverWideClaim.Epoch.ShouldEqual(new ClaimEpoch(7));
    [Fact] void should_keep_the_wide_resume_position() => _result.ObserverWideClaim.ResumePosition.ShouldEqual(_state.ObserverWideClaim.ResumePosition);
    [Fact] void should_keep_empty_wide_pending_work() => _result.ObserverWideClaim.PendingWork.ShouldBeEmpty();
    [Fact] void should_keep_the_revision() => _result.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_map_claims_with_failed_partitions() => _withFailedPartitions.PartitionClaims.Count().ShouldEqual(2);
    [Fact] void should_map_wide_claims_with_failed_partitions() => _withFailedPartitions.ObserverWideClaim.Job.ShouldEqual(_state.ObserverWideClaim.Job);
    [Fact] void should_map_revision_with_failed_partitions() => _withFailedPartitions.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_keep_legacy_claims_null() => _legacy.PartitionClaims.ShouldBeNull();
    [Fact] void should_keep_legacy_wide_claim_null() => _legacy.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_legacy_revision_null() => _legacy.OwnershipRevision.ShouldBeNull();
}
