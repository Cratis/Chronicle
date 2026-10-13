// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_scope_has_unfiltered_sentinels : given.a_before_first_batch
{
    void Establish()
    {
        _history.Add(EventToAppendFor(_eventSourceId) with
        {
            EventSourceType = new EventSourceType("another-source-type"),
            eventStreamId = new EventStreamId("another-stream"),
            eventStreamType = new EventStreamType("another-stream-type")
        });
        _scopes[_eventSourceId] = _scopes[_eventSourceId] with
        {
            EventTypes = [],
            EventSourceType = EventSourceType.Default,
            EventStreamId = EventStreamId.Default,
            EventStreamType = EventStreamType.All
        };
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_reject_a_source_with_history_in_a_named_stream() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_existing_source_violation() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_eventSourceId, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_not_append_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
