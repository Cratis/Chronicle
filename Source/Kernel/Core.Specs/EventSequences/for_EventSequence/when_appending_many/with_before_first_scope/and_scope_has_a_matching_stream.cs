// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_scope_has_a_matching_stream : given.a_before_first_batch
{
    void Establish()
    {
        _history.Add(EventToAppendFor(_eventSourceId));
        _scopes[_eventSourceId] = _scopes[_eventSourceId] with { EventStreamId = EventStreamId.Default, EventStreamType = EventStreamType.All };
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_reject_the_batch() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_matching_stream_violation() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_eventSourceId, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_not_append_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
