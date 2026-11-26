// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

public class and_only_one_of_the_event_sequences_holds_events : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish() => _logStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);

    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [],
        [UniqueEmail()]);

    [Fact] void should_only_reindex_the_outbox() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_refresh_the_log() => _log.Received(1).RefreshConstraints();
    [Fact] void should_refresh_the_outbox() => _outbox.Received(1).RefreshConstraints();
}
