// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using KernelState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_handing_over;

public class with_a_fresh_target : given.an_observer_state_storage
{
    KernelState _result;
    bool _accepted;

    async Task Establish() => await _storage.Save(new()
    {
        Identifier = "observer",
        NextEventSequenceNumber = new(999),
        IsReplaying = true,
        FailedPartitionCount = new(2),
        PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
        ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
        DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch.AddMinutes(5), [])]
    }, "A");

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", "A", ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _result = await _storage.Get("observer");
    }

    [Fact] void should_accept_the_handover() => _accepted.ShouldBeTrue();
    [Fact] void should_not_inherit_the_old_position() => _result.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_inherit_replay() => _result.IsReplaying.ShouldBeFalse();
    [Fact] void should_not_inherit_failures() => _result.FailedPartitionCount.Value.ShouldEqual(0);
    [Fact] void should_not_inherit_partition_claims() => _result.PartitionClaims.ShouldBeNull();
    [Fact] void should_not_inherit_the_wide_claim() => _result.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_not_inherit_fences() => _result.DeliveryFences.ShouldBeNull();
}
