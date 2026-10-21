// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_verifying_observations : Specification
{
    readonly Guid _id = Guid.NewGuid();

    [Fact]
    public void should_allow_redelivery_of_the_same_stored_event() =>
        ClusterLoad.VerifyObservations([(_id, (EventSequenceNumber)0)], [(_id, (EventSequenceNumber)0), (_id, (EventSequenceNumber)0)]);

    [Fact]
    public void should_reject_a_stored_event_that_was_not_handled() =>
        Catch.Exception(() => ClusterLoad.VerifyObservations([(_id, (EventSequenceNumber)0)], [])).ShouldNotBeNull();

    [Fact]
    public void should_reject_a_handled_event_that_was_not_stored() =>
        Catch.Exception(() => ClusterLoad.VerifyObservations([(_id, (EventSequenceNumber)0)], [(_id, (EventSequenceNumber)0), (Guid.NewGuid(), (EventSequenceNumber)1)])).ShouldNotBeNull();

    [Fact]
    public void should_reject_the_same_payload_id_at_a_different_sequence_number() =>
        Catch.Exception(() => ClusterLoad.VerifyObservations([(_id, (EventSequenceNumber)0)], [(_id, (EventSequenceNumber)0), (_id, (EventSequenceNumber)1)])).ShouldNotBeNull();
}
