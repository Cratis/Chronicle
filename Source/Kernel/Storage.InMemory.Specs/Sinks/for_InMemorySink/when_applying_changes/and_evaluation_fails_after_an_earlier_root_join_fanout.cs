// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_evaluation_fails_after_an_earlier_root_join_fanout : given.a_sink_with_two_joined_rows
{
    Exception? _error;
    long _countOnFirstRow;
    long _countOnSecondRow;
    bool _firstRowAdmitsARetry;

    void Establish() => _changeset.Changes.Returns(
    [
        RootJoin("first", "joinId", Set("count", 1L)),
        RootJoin("second", "joinId", _failing)
    ]);

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(KeyFor("a"), _changeset, WrittenAt));
        _countOnFirstRow = (long)Row("a")["count"]!;
        _countOnSecondRow = (long)Row("b")["count"]!;
        _firstRowAdmitsARetry = await AdmitsAGuardedWriteAtTheSameSequence("a");
    }

    [Fact] void should_propagate_the_evaluation_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_publish_the_earlier_joined_row() => _countOnFirstRow.ShouldEqual(0L);
    [Fact] void should_not_publish_the_failing_row() => _countOnSecondRow.ShouldEqual(0L);
    [Fact] void should_not_advance_the_watermark_of_the_earlier_joined_row() => _firstRowAdmitsARetry.ShouldBeTrue();
}
