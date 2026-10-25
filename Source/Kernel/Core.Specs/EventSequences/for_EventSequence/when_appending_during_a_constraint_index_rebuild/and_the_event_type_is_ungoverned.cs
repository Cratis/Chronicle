// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_during_a_constraint_index_rebuild;

public class and_the_event_type_is_ungoverned : given.a_rebuilding_constraint_index
{
    Task Establish() => _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        new("Unconstrained", EventTypeGeneration.First),
        new(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_accept_the_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_event() => _log.Events.Count.ShouldEqual(3);
    [Fact] async Task should_leave_the_rebuilding_index_untouched() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
