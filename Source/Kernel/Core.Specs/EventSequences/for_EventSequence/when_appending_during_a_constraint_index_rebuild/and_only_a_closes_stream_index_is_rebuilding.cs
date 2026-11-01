// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_during_a_constraint_index_rebuild;

public class and_only_a_closes_stream_index_is_rebuilding : given.a_rebuilding_constraint_index
{
    protected override bool Closing => true;

    Task Establish() => _eventSequence.ConstraintIndexRebuildStarting(_runId, [_closingDefinition.Name]);

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_accept_the_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_event() => _log.Events.Count.ShouldEqual(3);
    [Fact] async Task should_close_the_stream() => (await _closures.GetFor(_closingDefinition.Name.Value, new(EventSourceId: _eventSourceId))).ShouldNotBeNull();
}
