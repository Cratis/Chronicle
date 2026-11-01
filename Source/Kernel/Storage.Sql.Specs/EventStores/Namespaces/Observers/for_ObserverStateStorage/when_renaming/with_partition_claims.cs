// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_renaming;

public class with_partition_claims : given.an_observer_state_storage
{
    KernelObserverState _state;
    KernelObserverState _saved;

    async Task Establish()
    {
        _state = KernelObserverState.Empty with
        {
            Identifier = "observer",
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            OwnershipRevision = new(7)
        };
        await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Rename("observer", "renamed");
        _saved = await _storage.Get("renamed");
    }

    [Fact] void should_keep_the_partition() => _saved.PartitionClaims.Single().Partition.ShouldEqual(_state.PartitionClaims.Single().Partition);
    [Fact] void should_keep_the_partition_kind() => _saved.PartitionClaims.Single().Holder.ShouldEqual(ClaimHolderKind.PartitionCatchUp);
    [Fact] void should_keep_the_partition_job() => _saved.PartitionClaims.Single().Job.ShouldEqual(_state.PartitionClaims.Single().Job);
    [Fact] void should_keep_the_partition_epoch() => _saved.PartitionClaims.Single().Epoch.ShouldEqual(new ClaimEpoch(3));
    [Fact] void should_keep_the_resume_position() => _saved.PartitionClaims.Single().ResumePosition.ShouldEqual(_state.PartitionClaims.Single().ResumePosition);
    [Fact] void should_keep_the_observer_wide_job() => _saved.ObserverWideClaim.Job.ShouldEqual(_state.ObserverWideClaim.Job);
    [Fact] void should_keep_the_observer_wide_epoch() => _saved.ObserverWideClaim.Epoch.ShouldEqual(new ClaimEpoch(7));
    [Fact] void should_keep_the_revision() => _saved.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
}
