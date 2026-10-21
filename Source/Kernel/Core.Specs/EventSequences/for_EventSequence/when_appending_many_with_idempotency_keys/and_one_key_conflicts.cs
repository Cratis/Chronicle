// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_one_key_conflicts : given.an_idempotent_batch
{
    void Establish()
    {
        DurableAt(1, 4);
        _events[1].Content["changed"] = true;
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_whole_batch() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_conflicting_key() => _batchResult.Conflicts.Single().Key.ShouldEqual(_batchKeys[1]);
    [Fact] void should_report_content_conflict() => _batchResult.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Content);
    [Fact] void should_not_report_new_numbers() => _batchResult.Result.SequenceNumbers.ShouldBeEmpty();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
