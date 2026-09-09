// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_covered_removal_belongs_to_another_event_source : given.an_event_sequence_with_event_type_cycles
{
    void Establish() => _events = [EventFor(Covered), EventFor(CoveredRemoval, "other-source"), EventFor(Covered)];

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_only_reject_the_unreleased_event_source() => _result.ConstraintViolations.Select(violation => violation.EventTypeId).ShouldEqual([Covered.Id]);
    [Fact] void should_reject_the_entire_batch() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
