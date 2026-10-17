// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraint_store_fails_after_commit.for_a_reopening_event;

public class and_the_sequence_is_reactivated : given.an_event_sequence_with_repairable_indexes
{
    protected override bool Closing => true;
    EventSequenceNumber? _persistedBeforeActivation;

    async Task Establish()
    {
        await AppendAnEvent();
        _eventType = new("Reopened", EventTypeGeneration.First);
        _failWrites = true;
        await AppendAnEvent();
        await _eventSequence.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _persistedBeforeActivation = (await _log.GetState()).ConstraintIndexWatermark;
        _failWrites = false;
    }

    async Task Because()
    {
        await Reactivate();
        _eventType = new("Unrelated", EventTypeGeneration.First);
        _result = await AppendAnEvent();
    }

    [Fact] void should_reactivate_from_the_persisted_gap() => _persistedBeforeActivation.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_admit_after_repairing_the_reopening_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_remove_the_owned_closure() => (await _closures.GetFor("closing", new ClosedStreamScope { EventSourceId = _eventSourceId })).ShouldBeNull();
    [Fact] async Task should_persist_the_repaired_watermark() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
