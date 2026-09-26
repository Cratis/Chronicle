// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_no_events_conflict_with_a_scope : given.appending_many_events
{
    readonly EventSourceId _readSource = "read-source";
    AppendManyResult _result;

    void Establish()
    {
        _events.Clear();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _readSource, null, null, null)
            .Returns((EventSequenceNumber)5);
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_readSource] = new(4, true, null, null, null, [_eventType])
        }));

    [Fact] void should_report_the_conflict() =>
        _result.ConcurrencyViolations.ShouldContain(_ => _.EventSourceId == _readSource);

    [Fact] void should_not_append_anything() =>
        _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
