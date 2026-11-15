// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_direct_only_evaluation_fails : given.a_sink_with_two_joined_rows
{
    readonly List<object> _snapshots = [];
    IDisposable _subscription;
    Exception? _error;
    bool _retryAdmitted;
    string _nameAfterFailure;
    string _initialNameAfterFailure;
    int _notificationsAfterFailure;

    void Establish()
    {
        _changeset.Changes.Returns(
        [
            new PropertiesChanged<ExpandoObject>(
                _initial,
                [new PropertyDifference(new PropertyPath("[children].name"), "before", "after", new ArrayIndexers([new ArrayIndexer("[children]", "childId", "child")]))]),
            _failing
        ]);
        _subscription = _sink.ObserveInstances().Subscribe(_snapshots.Add);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(KeyFor("a"), _changeset, WrittenAt));
        _nameAfterFailure = (string)Children("a")[0]["name"]!;
        var initialChild = ((IEnumerable<object>)((IDictionary<string, object?>)_initial)["children"]!).Cast<IDictionary<string, object?>>().Single();
        _initialNameAfterFailure = (string)initialChild["name"]!;
        _notificationsAfterFailure = _snapshots.Count;
        _retryAdmitted = await AdmitsAGuardedWriteAtTheSameSequence("a");
    }

    [Fact] void should_propagate_the_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_leave_the_published_child_unchanged() => _nameAfterFailure.ShouldEqual("before");
    [Fact] void should_leave_the_initial_child_unchanged() => _initialNameAfterFailure.ShouldEqual("before");
    [Fact] void should_not_notify_on_evaluation_failure() => _notificationsAfterFailure.ShouldEqual(1);
    [Fact] void should_admit_a_same_sequence_retry() => _retryAdmitted.ShouldBeTrue();

    void Destroy() => _subscription.Dispose();
}
