// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_arrives_between_the_last_repair_ack_and_the_successor : given.an_observer_with_a_handover_log
{
    AppendedEvent _arrivingEvent;
    ObserverState _betweenAckAndTransfer;
    EventSequenceNumber[] _deliveredBeforeTransfer;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        Append("A", 10UL);
        var heldPrefix = Append("P", 11UL);
        Append("A", 13UL);
        var heldSuffix = Append("T", 15UL);
        _arrivingEvent = Append("Q", 16UL);
        await _observer.RegisterCatchingUpPartitions(["P", "T"]);
        await _observer.Handle("P", [heldPrefix]);
        await _observer.Handle("T", [heldSuffix]);
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        await _silo.RunCatchUpContinuation();
        await FinishRepair(_repairs.Single());
        await _observer.Handle("Q", [_arrivingEvent]);
        _deliveredBeforeTransfer = [.. _delivered];
        _betweenAckAndTransfer = PersistedObserverState;
        await _silo.RunCatchUpContinuation();
        if (_wide is { } wide) await DeliverRange(wide.FromEventSequenceNumber, EventSequenceNumber.Max, wide.EventTypes);
    }

    [Fact] void should_have_durably_discharged_the_last_repair() => _betweenAckAndTransfer.PendingCatchUpRepairs!.ShouldBeEmpty();
    [Fact] void should_not_send_q_before_the_transfer() => _deliveredBeforeTransfer.ShouldEqual([(EventSequenceNumber)11UL]);
    [Fact] void should_keep_the_cursor_frozen_after_the_last_acknowledgement() => _betweenAckAndTransfer.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_start_the_wide_reader_at_the_frozen_cursor() => _wide!.FromEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_deliver_each_repair_and_suffix_event_once() => _delivered.ShouldEqual([(EventSequenceNumber)11UL, (EventSequenceNumber)15UL, (EventSequenceNumber)16UL]);
}
