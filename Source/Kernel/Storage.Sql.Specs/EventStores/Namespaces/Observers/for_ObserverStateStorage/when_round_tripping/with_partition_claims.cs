// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_partition_claims : given.an_observer_state_storage
{
    KernelObserverState _state;
    KernelObserverState _saved;
    KernelObserverState _legacy;

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

    async Task Because()
    {
        await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
        await _storage.Save(KernelObserverState.Empty with { Identifier = "legacy" }, Concepts.EventSequences.EventSequenceId.Log);
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_partitions() => _saved.PartitionClaims.Select(claim => claim.Partition).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Partition));
    [Fact] void should_keep_kinds() => _saved.PartitionClaims.Select(claim => claim.Holder).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Holder));
    [Fact] void should_keep_null_and_non_null_jobs() => _saved.PartitionClaims.Select(claim => claim.Job).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Job));
    [Fact] void should_keep_epochs() => _saved.PartitionClaims.Select(claim => claim.Epoch).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Epoch));
    [Fact] void should_keep_resume_positions() => _saved.PartitionClaims.Select(claim => claim.ResumePosition).ShouldEqual(_state.PartitionClaims.Select(claim => claim.ResumePosition));
    [Fact] void should_keep_empty_pending_work() => _saved.PartitionClaims.All(claim => claim.PendingWork.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_the_wide_kind() => _saved.ObserverWideClaim.Holder.ShouldEqual(_state.ObserverWideClaim.Holder);
    [Fact] void should_keep_the_wide_job() => _saved.ObserverWideClaim.Job.ShouldEqual(_state.ObserverWideClaim.Job);
    [Fact] void should_keep_the_wide_epoch() => _saved.ObserverWideClaim.Epoch.ShouldEqual(new ClaimEpoch(7));
    [Fact] void should_keep_the_wide_resume_position() => _saved.ObserverWideClaim.ResumePosition.ShouldEqual(_state.ObserverWideClaim.ResumePosition);
    [Fact] void should_keep_empty_wide_pending_work() => _saved.ObserverWideClaim.PendingWork.ShouldBeEmpty();
    [Fact] void should_keep_the_revision() => _saved.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_keep_legacy_claims_null() => _legacy.PartitionClaims.ShouldBeNull();
    [Fact] void should_keep_legacy_wide_claim_null() => _legacy.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_legacy_revision_null() => _legacy.OwnershipRevision.ShouldBeNull();
}
