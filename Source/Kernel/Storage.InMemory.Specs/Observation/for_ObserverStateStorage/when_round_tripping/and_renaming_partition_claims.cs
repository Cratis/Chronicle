// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_round_tripping;

public class and_renaming_partition_claims : Specification
{
    ObserverStateStorage _storage;
    ObserverState _state;
    ObserverState _result;

    async Task Establish()
    {
        _storage = new();
        _state = ObserverState.Empty with
        {
            Identifier = "observer",
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, null, new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            OwnershipRevision = new(7)
        };
        await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Rename(_state.Identifier, new ObserverId("renamed"));
        _result = await _storage.Get(new ObserverId("renamed"));
    }

    [Fact] void should_keep_partition_claims() => _result.PartitionClaims.ShouldEqual(_state.PartitionClaims);
    [Fact] void should_keep_the_observer_wide_claim() => _result.ObserverWideClaim.ShouldEqual(_state.ObserverWideClaim);
    [Fact] void should_keep_the_revision() => _result.OwnershipRevision.ShouldEqual(_state.OwnershipRevision);

    void Destroy() => _storage.Dispose();
}
