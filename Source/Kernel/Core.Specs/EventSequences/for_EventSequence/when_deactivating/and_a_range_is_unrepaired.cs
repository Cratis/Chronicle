// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_deactivating;

public class and_a_range_is_unrepaired : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        await AppendAnEvent();
        _failWrites = true;
        await AppendAnEvent();
    }
    Task Because() => _eventSequence.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_leave_the_watermark_before_the_failed_event() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_keep_the_durable_tail() => _stateStorage.State.SequenceNumber.ShouldEqual((EventSequenceNumber)2);
}
