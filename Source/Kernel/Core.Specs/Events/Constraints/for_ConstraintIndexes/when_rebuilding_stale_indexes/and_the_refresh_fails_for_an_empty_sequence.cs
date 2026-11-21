// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

public class and_the_refresh_fails_for_an_empty_sequence : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish()
    {
        _logStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        _log.RefreshConstraints().Returns(Task.FromException(new InvalidOperationException()));
    }

    async Task Because() => await _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [],
        [UniqueEmail(EventSequenceId.Log)]);

    [Fact] void should_still_reindex_the_log() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Log);
}
