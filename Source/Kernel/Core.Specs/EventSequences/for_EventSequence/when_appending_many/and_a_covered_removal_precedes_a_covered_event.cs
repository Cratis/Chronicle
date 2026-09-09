// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_covered_removal_precedes_a_covered_event : given.an_event_sequence_with_event_type_cycles
{
    void Establish() => _events = [EventFor(CoveredRemoval), EventFor(Covered)];

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_accept_both_events() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_update_both_constraint_indexes_in_order() => _constraintIndexSequenceNumbers.ShouldEqual([(EventSequenceNumber)0, (EventSequenceNumber)1]);
}
