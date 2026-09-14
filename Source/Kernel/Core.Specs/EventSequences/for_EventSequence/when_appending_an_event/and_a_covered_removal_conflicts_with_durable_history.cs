// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_covered_removal_conflicts_with_durable_history : given.an_event_sequence_with_event_type_cycles
{
    AppendResult _appendResult;

    void Establish() =>
        _uniqueEventTypesStorage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), _eventSourceId, Arg.Any<ResolvedConstraintScope>())
            .Returns((false, EventSequenceNumber.First));

    async Task Because() => _appendResult = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        CoveredRemoval,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_reject_the_covered_removal() => _appendResult.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_persist_the_event() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_update_any_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
