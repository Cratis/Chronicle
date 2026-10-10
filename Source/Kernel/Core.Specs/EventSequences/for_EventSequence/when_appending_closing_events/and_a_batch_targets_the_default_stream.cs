// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events;

public class and_a_batch_targets_the_default_stream : given.a_stream_only_closing_constraint
{
    AppendManyResult _result;
    bool _closed;

    async Task Because()
    {
        _result = await _eventSequence.AppendMany(
            [new(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], new())],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
        _closed = (await _closures.GetAll()).Any();
    }

    [Fact] void should_refuse_the_batch() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_persist_no_batch() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_not_close_the_default_stream() => _closed.ShouldBeFalse();
}
