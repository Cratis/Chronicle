// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_an_observer_throws_during_a_same_row_join : given.a_sink_with_same_row_direct_and_join_changes
{
    readonly Exception _failure = new("Observer failed");
    readonly List<long> _snapshots = [];
    IDisposable _subscription;
    Exception? _error;
    bool _returnedTask;
    bool _observerSawDirectChange;

    void Establish()
    {
        _changeset.Changes.Returns(
        [
            new PropertiesChanged<ExpandoObject>(_initial, [new PropertyDifference(new PropertyPath("x"), null, 1L)]),
            Join()
        ]);
        _subscription = _sink.ObserveInstances().Subscribe(rows =>
        {
            _snapshots.Add((long)((IDictionary<string, object?>)rows.Single())["count"]!);
            if (_snapshots.Count > 1)
            {
                _observerSawDirectChange = ((IDictionary<string, object?>)rows.Single()).ContainsKey("x");
                throw _failure;
            }
        });
    }

    async Task Because()
    {
        _error = Catch.Exception(() =>
        {
            _ = _sink.ApplyChanges(_key, _changeset, 20UL);
            _returnedTask = true;
        });
        _changeset.Changes.Returns([Direct(3L)]);
        await _sink.ApplyChanges(_key, _changeset, 20UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
    }

    [Fact] void should_propagate_the_observer_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_throw_before_returning_a_task() => _returnedTask.ShouldBeFalse();
    [Fact] void should_keep_the_committed_join_on_a_same_sequence_retry() => StoredCount.ShouldEqual(2L);
    [Fact] void should_have_published_the_direct_change() => ((IDictionary<string, object?>)_sink.Collection["root"])["x"].ShouldEqual(1L);
    [Fact] void should_show_the_completed_state_to_the_observer() => _observerSawDirectChange.ShouldBeTrue();
    [Fact] void should_not_notify_again_on_a_same_sequence_retry() => _snapshots.ShouldEqual(new[] { 0L, 2L });

    void Destroy() => _subscription.Dispose();
}
