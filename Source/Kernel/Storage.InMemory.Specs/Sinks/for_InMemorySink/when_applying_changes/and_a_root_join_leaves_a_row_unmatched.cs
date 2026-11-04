// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_root_join_leaves_a_row_unmatched : given.a_sink_with_two_joined_rows
{
    IDictionary<string, object?> _originalMatched;
    IDictionary<string, object?> _originalUnmatched;
    IDictionary<string, object?> _unmatchedAfterJoin;
    bool _matchedRetryAdmitted;
    bool _unmatchedRetryAdmitted;

    void Establish()
    {
        _originalMatched = Row("a");
        _originalUnmatched = Row("b");
        _changeset.Changes.Returns([RootJoin("first", "joinId", Set("count", 1L))]);
    }

    async Task Because()
    {
        await _sink.ApplyChanges(KeyFor("first"), _changeset, WrittenAt);
        _unmatchedAfterJoin = Row("b");
        _matchedRetryAdmitted = await AdmitsAGuardedWriteAtTheSameSequence("a");
        _unmatchedRetryAdmitted = await AdmitsAGuardedWriteAtTheSameSequence("b");
    }

    [Fact] void should_update_the_matching_row() => Row("a")["count"].ShouldEqual(1L);
    [Fact] void should_detach_the_matching_row() => Row("a").ShouldNotBeSame(_originalMatched);
    [Fact] void should_leave_the_original_matching_row_unchanged() => _originalMatched["count"].ShouldEqual(0L);
    [Fact] void should_not_copy_the_unmatched_row() => _unmatchedAfterJoin.ShouldBeSame(_originalUnmatched);
    [Fact] void should_advance_the_matching_watermark() => _matchedRetryAdmitted.ShouldBeFalse();
    [Fact] void should_not_advance_the_unmatched_watermark() => _unmatchedRetryAdmitted.ShouldBeTrue();
}
