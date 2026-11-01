// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverSequenceCheckpoint;

public class when_creating_a_fresh_checkpoint : Specification
{
    ObserverSequenceCheckpoint _result;

    void Because() => _result = ObserverSequenceCheckpoint.Fresh("observer", "B");

    [Fact] void should_identify_the_observer() => _result.ObserverId.ShouldEqual(new ObserverId("observer"));
    [Fact] void should_identify_the_sequence() => _result.EventSequenceId.ShouldEqual(new EventSequenceId("B"));
    [Fact] void should_start_at_the_first_event() => _result.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_have_no_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_have_no_tail() => _result.TailEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_have_no_failures() => _result.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_not_replay() => _result.IsReplaying.ShouldBeFalse();
    [Fact] void should_have_no_partition_claims() => _result.PartitionClaims.ShouldBeNull();
    [Fact] void should_have_no_wide_claim() => _result.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_have_no_fences() => _result.DeliveryFences.ShouldBeNull();
}
