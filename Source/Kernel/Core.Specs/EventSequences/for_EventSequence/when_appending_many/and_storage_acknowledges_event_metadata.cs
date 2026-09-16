// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_storage_acknowledges_event_metadata : given.appending_many_events
{
    AppendedEvent[] _storedEvents;
    AppendManyResult _result;

    void Establish() => _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
    {
        _storedEvents = AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>()).ToArray();
        return Task.FromResult<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>>(_storedEvents);
    });

    async Task Because() => _result = await _eventSequence.AppendMany(_events, CorrelationId.NotSet, [], Identity.NotSet, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()), includeReceipts: true);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_a_receipt_for_each_event() => _result.Receipts.Count().ShouldEqual(3);
    [Fact] void should_preserve_input_order() => _result.Receipts.Select(_ => _.EventSourceId).ShouldEqual(_events.Select(_ => _.EventSourceId));
    [Fact] void should_match_the_returned_sequence_numbers() => _result.Receipts.Select(_ => _.SequenceNumber).ShouldEqual(_result.SequenceNumbers);
    [Fact] void should_use_the_persisted_occurrence_times() => _result.Receipts.Select(_ => _.Occurred).ShouldEqual(_storedEvents.Select(_ => _.Context.Occurred));
    [Fact] void should_preserve_receipts_while_reporting_concurrency() => _result.ReportingConcurrencyCheck(true).Receipts.ShouldEqual(_result.Receipts);
}
