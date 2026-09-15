// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_disjoint_removals_release_multiple_constraints : given.an_event_sequence_with_event_type_cycles
{
    void Establish()
    {
        _definitions =
        [
            new("first-cycle", [Covered.Id], [Removal.Id, OtherRemoval.Id]),
            new("second-cycle", [Covered.Id], [Removal.Id, OtherRemoval.Id])
        ];
        _events = [EventFor(Covered), EventFor(Removal), EventFor(Covered), EventFor(OtherRemoval), EventFor(Covered)];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_accept_each_new_cycle() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_update_every_constraint_index_in_order() => _constraintIndexSequenceNumbers.ShouldEqual([(EventSequenceNumber)0, (EventSequenceNumber)1, (EventSequenceNumber)2, (EventSequenceNumber)3, (EventSequenceNumber)4]);
}
