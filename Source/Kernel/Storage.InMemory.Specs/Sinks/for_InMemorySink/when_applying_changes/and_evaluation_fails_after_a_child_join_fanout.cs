// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_evaluation_fails_after_a_child_join_fanout : given.a_sink_with_two_joined_rows
{
    Exception? _error;
    string[] _childNames;
    bool _firstRowAdmitsARetry;
    bool _secondRowAdmitsARetry;

    void Establish() => _changeset.Changes.Returns([ChildJoin(RenameChild()), _failing]);

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(KeyFor("a"), _changeset, WrittenAt));
        _childNames = [.. Children("a").Concat(Children("b")).Select(child => (string)child["name"]!)];
        _firstRowAdmitsARetry = await AdmitsAGuardedWriteAtTheSameSequence("a");
        _secondRowAdmitsARetry = await AdmitsAGuardedWriteAtTheSameSequence("b");
    }

    [Fact] void should_propagate_the_evaluation_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_publish_the_joined_children() => _childNames.ShouldEqual(["before", "before"]);
    [Fact] void should_not_advance_the_watermark_of_the_first_parent() => _firstRowAdmitsARetry.ShouldBeTrue();
    [Fact] void should_not_advance_the_watermark_of_the_second_parent() => _secondRowAdmitsARetry.ShouldBeTrue();
}
