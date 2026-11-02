// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_direct_evaluation_fails_after_a_same_row_join : given.a_sink_with_same_row_direct_and_join_changes
{
    readonly Exception _failure = new("Nested evaluation failed");
    readonly List<long> _snapshots = [];
    IDisposable _subscription;
    Exception? _error;
    long _countAfterFailure;
    long[] _snapshotsAfterFailure;

    void Establish()
    {
        var nestedChanges = Substitute.For<IEnumerable<Change>>();
        nestedChanges.GetEnumerator().Returns(_ => throw _failure);
        _changeset.Changes.Returns(
        [
            Direct(1L),
            Join(),
            new ResolvedJoin(_initial, "root", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, nestedChanges)
        ]);
        _subscription = _sink.ObserveInstances().Subscribe(rows =>
            _snapshots.Add((long)((IDictionary<string, object?>)rows.Single())["count"]!));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(_key, _changeset, 20UL));
        _countAfterFailure = StoredCount;
        _snapshotsAfterFailure = [.. _snapshots];
        _changeset.Changes.Returns([Direct(3L)]);
        await _sink.ApplyChanges(_key, _changeset, 20UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
    }

    [Fact] void should_propagate_the_evaluation_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_leave_the_published_state_unchanged() => _countAfterFailure.ShouldEqual(0L);
    [Fact] void should_not_notify_on_evaluation_failure() => _snapshotsAfterFailure.ShouldEqual([0L]);
    [Fact] void should_admit_a_same_sequence_retry_after_evaluation_failure() => StoredCount.ShouldEqual(3L);

    void Destroy() => _subscription.Dispose();
}
