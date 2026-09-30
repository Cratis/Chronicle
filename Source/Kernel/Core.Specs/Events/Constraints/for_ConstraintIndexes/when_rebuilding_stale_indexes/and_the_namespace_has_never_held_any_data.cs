// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// A namespace that has never held data has no events to index, so it is skipped rather than materialized.
/// </summary>
public class and_the_namespace_has_never_held_any_data : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish() => _namespaceStorage.HasData().Returns(false);

    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [UniqueEmail(EventSequenceId.Log)],
        [UniqueEmail()]);

    [Fact] void should_not_start_any_reindex() => _startedReindexes.ShouldBeEmpty();
    [Fact] void should_not_ask_for_the_event_sequences() => _eventSequences.DidNotReceive().GetEventSequences();
}
