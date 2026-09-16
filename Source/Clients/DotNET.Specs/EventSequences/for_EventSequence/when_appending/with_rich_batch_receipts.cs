// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_rich_batch_receipts : given.an_acknowledged_append
{
    AppendManyResult _result;

    void Establish()
    {
        var second = _batchResponse.Receipts.Last();
        second.EventSourceId = "second-source";
        second.Causation = [];
        second.CausedBy = new() { Subject = "second-caller", Name = "Second Caller", UserName = "second-user" };
        second.ObservationState = Contracts.Events.EventObservationState.None;
        second.Hash = EventHash.NotSet;
    }

    async Task Because() => _result = await _eventSequence.AppendMany([new(_source, "first"), new("second-source", "second")]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_both_receipts() => _result.Receipts.Count.ShouldEqual(2);
    [Fact] void should_notify_each_input_with_its_receipt() => _notifications.Select(_ => _.Event.Context).ShouldEqual(_result.Receipts);
    [Fact] void should_preserve_input_order() => _notifications.Select(_ => _.Event.Content).ShouldEqual(["first", "second"]);
    [Fact] void should_preserve_persisted_sources() => _result.Receipts.Select(_ => _.EventSourceId.Value).ShouldEqual([_source.Value, "second-source"]);
    [Fact] void should_preserve_each_causation_chain() => _result.Receipts.Select(_ => _.Causation.Count()).ShouldEqual([1, 0]);
    [Fact] void should_preserve_each_identity() => _result.Receipts.Select(_ => _.CausedBy.Subject).ShouldEqual(["stored-caller", "second-caller"]);
    [Fact] void should_preserve_each_observation_state() => _result.Receipts.Select(_ => _.ObservationState).ShouldEqual([EventObservationState.Initial, EventObservationState.None]);
    [Fact] void should_not_invent_an_unavailable_hash() => _result.Receipts[^1].Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_carry_each_receipt_on_its_notification_result() => _notifications.All(_ => _.Result.Receipt == _.Event.Context).ShouldBeTrue();
    [Fact] void should_use_each_receipts_correlation_for_notifications() => _notifications.All(_ => _.Result.CorrelationId == _.Event.Context.CorrelationId).ShouldBeTrue();
}
