// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints.for_ObserverSequenceCheckpointConverters;

public class when_round_tripping : Specification
{
    ObserverSequenceCheckpoint _expected;
    ObserverSequenceCheckpoint _result;
    ObserverSequenceCheckpoint _fresh;

    void Establish() => _expected = ObserverSequenceCheckpoint.Fresh("observer", "A") with
    {
        NextEventSequenceNumber = new(43),
        LastHandledEventSequenceNumber = new(42),
        TailEventSequenceNumber = new(80),
        FailedPartitionCount = new(2),
        IsReplaying = true,
        PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
        ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
        DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch.AddMinutes(5), [])],
        Parked = DateTimeOffset.UnixEpoch.AddMinutes(10)
    };

    void Because()
    {
        _result = _expected.ToEntity().ToKernel();
        _fresh = ObserverSequenceCheckpoint.Fresh("observer", "B").ToEntity().ToKernel();
    }

    [Fact] void should_keep_the_observer() => _result.ObserverId.ShouldEqual(_expected.ObserverId);
    [Fact] void should_keep_the_sequence() => _result.EventSequenceId.ShouldEqual(_expected.EventSequenceId);
    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual(_expected.NextEventSequenceNumber);
    [Fact] void should_keep_the_last_position() => _result.LastHandledEventSequenceNumber.ShouldEqual(_expected.LastHandledEventSequenceNumber);
    [Fact] void should_keep_the_tail() => _result.TailEventSequenceNumber.ShouldEqual(_expected.TailEventSequenceNumber);
    [Fact] void should_keep_the_failed_count() => _result.FailedPartitionCount.ShouldEqual(_expected.FailedPartitionCount);
    [Fact] void should_keep_replay() => _result.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_partition_claims() => _result.PartitionClaims.Single().ResumePosition.ShouldEqual(_expected.PartitionClaims.Single().ResumePosition);
    [Fact] void should_keep_the_wide_claim() => _result.ObserverWideClaim.ResumePosition.ShouldEqual(_expected.ObserverWideClaim.ResumePosition);
    [Fact] void should_keep_fences() => _result.DeliveryFences.Single().ReleaseBy.ShouldEqual(_expected.DeliveryFences.Single().ReleaseBy);
    [Fact] void should_keep_the_complete_partition_claims() => _result.ToEntity().PartitionClaims.ShouldEqual(_expected.ToEntity().PartitionClaims);
    [Fact] void should_keep_the_complete_wide_claim() => _result.ToEntity().ObserverWideClaim.ShouldEqual(_expected.ToEntity().ObserverWideClaim);
    [Fact] void should_keep_the_complete_fences() => _result.ToEntity().DeliveryFences.ShouldEqual(_expected.ToEntity().DeliveryFences);
    [Fact] void should_keep_the_parking_time() => _result.Parked.ShouldEqual(_expected.Parked);
    [Fact] void should_keep_absent_claims_null() => _fresh.PartitionClaims.ShouldBeNull();
    [Fact] void should_keep_an_absent_wide_claim_null() => _fresh.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_absent_fences_null() => _fresh.DeliveryFences.ShouldBeNull();
}
