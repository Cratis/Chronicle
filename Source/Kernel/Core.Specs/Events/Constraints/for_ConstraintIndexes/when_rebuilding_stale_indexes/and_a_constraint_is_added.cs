// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// A new constraint is indexed from the events already appended, for every sequence it applies to and none other.
/// </summary>
public class and_a_constraint_is_added : given.an_event_store_with_a_log_and_an_outbox
{
    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [],
        [UniqueEmail(EventSequenceId.Log)]);

    [Fact] void should_start_a_single_reindex() => _startedReindexes.Count.ShouldEqual(1);
    [Fact] void should_reindex_the_event_log() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Log);
}
