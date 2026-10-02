// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_concurrency_rejection_requires_history_reload : given.an_alert_tracker
{
    async Task Establish()
    {
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, new ConcurrencyViolation(EventSourceId.Unspecified, EventSequenceNumber.BeforeFirst, 0UL)));
        await _tracker.Reconcile(_snapshot);
        GivenHistory(RaisedForSnapshot());
        _sequence.ClearReceivedCalls();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] async Task should_reload_the_durable_transitions() => await _sequenceStorage.Received(2).GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_not_repeat_the_raise_committed_by_the_other_writer() => _sequence.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).ShouldBeEmpty();
    [Fact] void should_acknowledge_the_durable_state() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
