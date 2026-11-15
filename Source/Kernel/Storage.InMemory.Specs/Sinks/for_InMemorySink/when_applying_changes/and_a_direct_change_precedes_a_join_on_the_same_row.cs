// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_direct_change_precedes_a_join_on_the_same_row : given.a_sink_with_same_row_direct_and_join_changes
{
    void Establish() => _changeset.Changes.Returns([Direct(1L), Join()]);

    async Task Because() => await _sink.ApplyChanges(_key, _changeset, EventSequenceNumber.Unavailable);

    [Fact] void should_keep_the_last_joined_value() => StoredCount.ShouldEqual(2L);
    [Fact] void should_leave_one_row() => _sink.Collection.Count.ShouldEqual(1);
}
