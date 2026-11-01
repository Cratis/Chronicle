// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverSequenceCheckpoint;

public class when_parking_an_observer : Specification
{
    ObserverState _state;
    ObserverSequenceCheckpoint _result;

    void Establish() => _state = new()
    {
        Identifier = "observer",
        NextEventSequenceNumber = new(43),
        LastHandledEventSequenceNumber = new(42),
        TailEventSequenceNumber = new(80),
        FailedPartitionCount = new(2),
        IsReplaying = true,
        HandledEventCount = new(999),
        PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
        ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
        DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch.AddMinutes(5), [])]
    };

    void Because() => _result = ObserverSequenceCheckpoint.ParkedFrom(_state, "A");

    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual(_state.NextEventSequenceNumber);
    [Fact] void should_keep_the_last_position() => _result.LastHandledEventSequenceNumber.ShouldEqual(_state.LastHandledEventSequenceNumber);
    [Fact] void should_keep_the_tail() => _result.TailEventSequenceNumber.ShouldEqual(_state.TailEventSequenceNumber);
    [Fact] void should_keep_the_failed_count() => _result.FailedPartitionCount.ShouldEqual(_state.FailedPartitionCount);
    [Fact] void should_keep_replay() => _result.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_partition_claims() => _result.PartitionClaims.ShouldEqual(_state.PartitionClaims);
    [Fact] void should_keep_the_wide_claim() => _result.ObserverWideClaim.ShouldEqual(_state.ObserverWideClaim);
    [Fact] void should_keep_fences() => _result.DeliveryFences.ShouldEqual(_state.DeliveryFences);
    [Fact] void should_not_replace_the_observer_position() => _state.NextEventSequenceNumber.ShouldEqual(new EventSequenceNumber(43));
}
