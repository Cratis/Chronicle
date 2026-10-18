// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class with_stream_conjunctions_and_a_renumbered_retry : given.appending_many_events
{
    AppendManyResult _result;
    bool _collided;

    void Establish()
    {
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source-1", null, (EventStreamId)"first", null).Returns(_ => (EventSequenceNumber)(_collided ? 6UL : 5UL));
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source-1", null, (EventStreamId)"second", null).Returns(_ => (EventSequenceNumber)(_collided ? 7UL : 5UL));
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
        {
            _collided = true;
            return (Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>)new DuplicateEventSequenceNumber(10UL);
        });
    }

    async Task Because() => _result = await _eventSequence.AppendManyWithConjunctions(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopeConjunctions([("source-1", new(5, true, null, "first", null, null), 0), ("source-1", new(5, true, null, "second", null, null), 1)]));

    [Fact] void should_recheck_both_guards() => _result.ConcurrencyViolations.Select(_ => _.ScopeIndex).ShouldContainOnly((uint?)0, (uint?)1);
    [Fact] void should_not_retry_the_write_after_a_conflict() => _eventSequenceStorage.Received(1).AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
