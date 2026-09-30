// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// The outbox stops validating and indexing the constraint, so its index is no longer read, and the event log is
/// covered before and after. Nothing needs rebuilding.
/// </summary>
public class and_a_constraint_was_narrowed_to_the_event_log : given.an_event_store_with_a_log_and_an_outbox
{
    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [UniqueEmail()],
        [UniqueEmail(EventSequenceId.Log)]);

    [Fact] void should_not_start_any_reindex() => _startedReindexes.ShouldBeEmpty();
}
