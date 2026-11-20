// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

public class and_an_event_arrives_before_the_refresh_completes : given.an_event_store_with_a_log_and_an_outbox
{
    TaskCompletionSource _refresh;
    string[] _callsWhileRefreshing;

    void Establish()
    {
        _refresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _logStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        _log.RefreshConstraints().Returns(_ =>
        {
            _calls.Add($"refresh {EventSequenceId.Log}");
            return _refresh.Task;
        });
    }

    async Task Because()
    {
        _calls.Clear();
        var rebuilding = _constraintIndexes.RebuildStaleIndexes(_eventStore, [], [UniqueEmail(EventSequenceId.Log)]);
        _callsWhileRefreshing = [.. _calls];

        // Model an append becoming durable before the refresh finishes.
        _logStorage.GetTailSequenceNumber().Returns(_ =>
        {
            _calls.Add($"tail {EventSequenceId.Log}");
            return Task.FromResult(EventSequenceNumber.First);
        });
        _refresh.SetResult();
        await rebuilding;
    }

    [Fact] void should_wait_for_the_refresh_before_reading_the_tail() => _callsWhileRefreshing.ShouldEqual(new[] { $"refresh {EventSequenceId.Log}" });
    [Fact] void should_reindex_the_event_that_arrived() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_check_the_tail_after_refreshing_and_before_reindexing() => _calls.ShouldEqual(new[] { $"refresh {EventSequenceId.Log}", $"tail {EventSequenceId.Log}", $"reindex {EventSequenceId.Log}" });
}
