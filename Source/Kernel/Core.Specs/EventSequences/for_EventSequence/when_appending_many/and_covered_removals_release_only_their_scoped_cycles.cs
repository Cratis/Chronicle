// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_covered_removals_release_only_their_scoped_cycles : given.an_event_sequence_with_event_type_cycles
{
    void Establish()
    {
        _definitions = [new("cycle", [Covered.Id, CoveredRemoval.Id], [CoveredRemoval.Id], new ConstraintScope(EventStreamId: (EventStreamId)"_scoped_"))];
        _events =
        [
            EventFor(CoveredRemoval, eventStreamId: "first"),
            EventFor(Covered, eventStreamId: "second"),
            EventFor(CoveredRemoval, eventStreamId: "first"),
            EventFor(Covered, eventStreamId: "first")
        ];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_accept_the_independent_cycles() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_update_every_constraint_index_in_order() => _constraintIndexSequenceNumbers.ShouldEqual([(EventSequenceNumber)0, (EventSequenceNumber)1, (EventSequenceNumber)2, (EventSequenceNumber)3]);
}
