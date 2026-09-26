// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_read_scope_targets_another_source : given.appending_many_events
{
    readonly EventSourceId _readSource = "read-source";
    AppendManyResult _result;

    void Establish() =>
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _readSource, null, null, null)
            .Returns((EventSequenceNumber)5);

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_readSource] = new(4, true, null, null, null, [_eventType])
        }));

    [Fact] void should_report_the_read_source_conflict() =>
        _result.ConcurrencyViolations.ShouldContain(_ => _.EventSourceId == _readSource);

    [Fact] void should_not_append_events_for_the_other_sources() =>
        _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
