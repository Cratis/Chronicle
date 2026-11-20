// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

public class and_the_emptiness_check_fails : given.an_event_store_with_a_log_and_an_outbox
{
    Exception _error;

    void Establish()
    {
        _logStorage.GetTailSequenceNumber().Returns(Task.FromException<EventSequenceNumber>(new InvalidOperationException()));
        _calls.Clear();
    }

    async Task Because() => _error = await Catch.Exception(() => _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [],
        [UniqueEmail(EventSequenceId.Log)]));

    [Fact] void should_still_reindex_the_log() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_not_fail_registration() => _error.ShouldBeNull();
    [Fact] void should_refresh_before_reindexing() => _calls.ShouldEqual(new[] { $"refresh {EventSequenceId.Log}", $"reindex {EventSequenceId.Log}" });
}
