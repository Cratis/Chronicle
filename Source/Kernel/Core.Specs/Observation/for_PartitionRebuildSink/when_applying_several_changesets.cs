// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_applying_several_changesets : given.a_partition_rebuild_sink
{
    readonly ExpandoObject _initial = new();
    ExpandoObject _firstState;
    ExpandoObject _secondState;

    void Establish() => ((IDictionary<string, object?>)_initial)["children"] = new List<object>();

    async Task Because()
    {
        await AddChild(_initial, "first", 1);
        _firstState = _sink.State!;
        await AddChild(_sink.State!, "second", 2);
        _secondState = _sink.State!;
        await AddChild(_sink.State!, "third", 3);
    }

    [Fact] void should_hold_all_changes() => ((IEnumerable<object>)((IDictionary<string, object?>)_sink.State!)["children"]!).ShouldContainOnly("first", "second", "third");
    [Fact] void should_copy_the_initial_state() => _firstState.ShouldNotBeSame(_initial);
    [Fact] void should_leave_the_initial_children_untouched() => ((IEnumerable<object>)((IDictionary<string, object?>)_initial)["children"]!).ShouldBeEmpty();
    [Fact] void should_reuse_the_state_after_the_first_copy() => _secondState.ShouldBeSame(_firstState);
    [Fact] void should_keep_the_state_applied_in_place() => _sink.State.ShouldBeSame(_secondState);
    [Fact] void should_track_the_last_watermark() => _sink.Watermark.ShouldEqual((EventSequenceNumber)3);

    Task AddChild(ExpandoObject initial, string child, EventSequenceNumber sequenceNumber)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), AppendedEvent.Empty(), initial);
        changeset.AddChild(new PropertyPath("children"), child);
        return _sink.ApplyChanges(_partition, changeset, sequenceNumber);
    }
}
