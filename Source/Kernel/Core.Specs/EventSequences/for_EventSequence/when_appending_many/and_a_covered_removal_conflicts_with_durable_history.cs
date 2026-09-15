// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_covered_removal_conflicts_with_durable_history : given.an_event_sequence_with_event_type_cycles
{
    void Establish()
    {
        // A covered event for this source already holds the durable cycle.
        _uniqueEventTypesStorage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), _eventSourceId, Arg.Any<ResolvedConstraintScope>())
            .Returns((false, EventSequenceNumber.First));
        _events = [EventFor(CoveredRemoval)];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_reject_the_covered_removal() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_persist_any_events() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_not_update_any_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
