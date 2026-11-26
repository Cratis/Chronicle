// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

public class and_the_event_sequence_holds_no_events : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish() => _logStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);

    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [],
        [UniqueEmail(EventSequenceId.Log)]);

    [Fact] void should_not_start_a_reindex() => _startedReindexes.ShouldBeEmpty();
    [Fact] void should_refresh_the_constraints() => _log.Received(1).RefreshConstraints();
    [Fact] void should_record_the_refresh() => _calls.ShouldContain($"refresh {EventSequenceId.Log}");
}
