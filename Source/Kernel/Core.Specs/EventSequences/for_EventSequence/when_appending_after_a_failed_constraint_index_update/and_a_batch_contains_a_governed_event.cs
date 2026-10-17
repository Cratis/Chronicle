// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_a_batch_contains_a_governed_event : given.an_unfinished_constraint_repair
{
    AppendManyResult _batch;

    void Establish() => _complianceManager.ClearReceivedCalls();

    async Task Because() => _batch = await _eventSequence.AppendMany(
        [ValidatedEvent().Event with { EventType = new("Unrelated", EventTypeGeneration.First) }, ValidatedEvent().Event with { EventType = _eventType }],
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_refuse_the_entire_batch() => _batch.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_create_keys_even_for_the_first_non_governed_event() => _complianceManager.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_store_any_of_the_batch() => _log.Events.Count.ShouldEqual(1);
}
