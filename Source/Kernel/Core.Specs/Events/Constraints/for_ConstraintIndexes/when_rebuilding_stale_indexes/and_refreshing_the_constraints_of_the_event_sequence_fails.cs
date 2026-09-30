// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// The sequence refreshes on its own within the throttle, so an index rebuilt with that window open is still better
/// than no rebuild at all.
/// </summary>
public class and_refreshing_the_constraints_of_the_event_sequence_fails : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish() => _outbox.RefreshConstraints().Returns(Task.FromException(new InvalidOperationException()));

    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [UniqueEmail(EventSequenceId.Log)],
        [UniqueEmail()]);

    [Fact] void should_still_reindex_the_outbox() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Outbox);
}
