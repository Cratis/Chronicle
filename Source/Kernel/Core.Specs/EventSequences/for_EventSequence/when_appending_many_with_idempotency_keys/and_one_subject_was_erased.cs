// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_one_subject_was_erased : given.an_idempotent_batch
{
    async Task Establish()
    {
        await ProtectBatch(erased: true);
        _request.Content["name"] = "a guess";
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_erased_outcome() => _batchResult.Outcomes.ShouldEqual([AppendOutcome.AlreadyAppendedAfterErasure, AppendOutcome.Appended, AppendOutcome.Appended]);
    [Fact] void should_only_append_new_events() => _written.Count.ShouldEqual(2);
}
