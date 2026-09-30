// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// The outbox's index is missing every value appended while the constraint was scoped to the event log, so it is
/// rebuilt - whether or not the outbox's grain is active, since no grain is asked.
/// </summary>
public class and_a_constraint_was_widened_to_the_outbox : given.an_event_store_with_a_log_and_an_outbox
{
    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [UniqueEmail(EventSequenceId.Log)],
        [UniqueEmail()]);

    [Fact] void should_start_a_single_reindex() => _startedReindexes.Count.ShouldEqual(1);
    [Fact] void should_reindex_the_outbox() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_reindex_the_widened_constraint() => _startedReindexes.Single().Changes.Single().Name.Value.ShouldEqual("UniqueEmail");
    [Fact] void should_report_the_event_sequences_as_changed() => _startedReindexes.Single().Changes.Single().ChangeTypes.ShouldContainOnly([ConstraintChangeType.EventSequencesChanged]);
}
