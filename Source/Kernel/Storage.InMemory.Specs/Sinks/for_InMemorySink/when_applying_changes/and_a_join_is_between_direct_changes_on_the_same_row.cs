// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_join_is_between_direct_changes_on_the_same_row : given.a_sink_with_same_row_direct_and_join_changes
{
    void Establish() => _changeset.Changes.Returns(
    [
        new PropertiesChanged<ExpandoObject>(_initial,
        [
            new PropertyDifference(new PropertyPath("count"), 0L, 1L),
            new PropertyDifference(new PropertyPath("x"), null, 1L)
        ]),
        Join(),
        Direct(3L)
    ]);

    async Task Because() => await _sink.ApplyChanges(_key, _changeset, EventSequenceNumber.Unavailable);

    [Fact] void should_keep_the_last_direct_value() => StoredCount.ShouldEqual(3L);
    [Fact] void should_keep_the_first_direct_only_property() => ((IDictionary<string, object?>)_sink.Collection["root"])["x"].ShouldEqual(1L);
    [Fact] void should_keep_the_untouched_property() => ((IDictionary<string, object?>)_sink.Collection["root"])["retained"].ShouldEqual("untouched");
    [Fact] void should_leave_one_row() => _sink.Collection.Count.ShouldEqual(1);
}
