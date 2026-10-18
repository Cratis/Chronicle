// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class with_a_violated_stream_conjunction : given.appending_many_events
{
    AppendManyResult _result;

    void Establish()
    {
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source-1", null, (EventStreamId)"first", null).Returns((EventSequenceNumber)5UL);
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source-1", null, (EventStreamId)"second", null).Returns((EventSequenceNumber)6UL);
    }

    async Task Because() => _result = await _eventSequence.AppendManyWithConjunctions(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopeConjunctions([("source-1", new(5, true, null, "first", null, null), 0), ("source-1", new(5, true, null, "second", null, null), 1)]));

    [Fact] void should_refuse_the_entire_batch() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_failed_guard() => _result.ConcurrencyViolations.Single().ScopeIndex.ShouldEqual((uint?)1);
    [Fact] void should_write_nothing() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
