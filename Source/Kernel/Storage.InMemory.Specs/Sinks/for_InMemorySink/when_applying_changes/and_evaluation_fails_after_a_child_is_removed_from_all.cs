// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_evaluation_fails_after_a_child_is_removed_from_all : given.a_sink_with_two_joined_rows
{
    Exception? _error;
    int _childrenOnFirstRow;
    int _childrenOnSecondRow;

    void Establish() => _changeset.Changes.Returns([RemoveChildFromAll(), _failing]);

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(KeyFor("a"), _changeset, WrittenAt));
        _childrenOnFirstRow = Children("a").Length;
        _childrenOnSecondRow = Children("b").Length;
    }

    [Fact] void should_propagate_the_evaluation_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_keep_the_child_on_the_row_being_written() => _childrenOnFirstRow.ShouldEqual(1);
    [Fact] void should_keep_the_child_on_the_other_row() => _childrenOnSecondRow.ShouldEqual(1);
}
