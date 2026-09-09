// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_removal_violates_another_constraint : given.an_event_sequence_with_event_type_cycles
{
    void Establish()
    {
        _definitions =
        [
            new("cycle", [Covered.Id], [Removal.Id]),
            new("removal-limit", [Removal.Id])
        ];
        _uniqueEventTypesStorage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), _eventSourceId, Arg.Any<ResolvedConstraintScope>())
            .Returns((false, EventSequenceNumber.First));
        _events = [EventFor(Removal), EventFor(Covered)];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_not_release_the_cycle_for_the_subsequent_event() => _result.ConstraintViolations.Count().ShouldEqual(2);
    [Fact] void should_reject_the_entire_batch() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_not_update_any_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
