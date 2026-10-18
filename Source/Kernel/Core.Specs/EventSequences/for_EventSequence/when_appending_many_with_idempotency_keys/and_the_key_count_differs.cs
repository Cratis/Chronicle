// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_the_key_count_differs : given.an_idempotent_batch
{
    void Establish() => _batchKeys.RemoveAt(2);
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_whole_batch() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_alignment() => _batchResult.Result.Errors.Single().Value.ShouldEqual("The idempotency key count must match the event count. Nothing was written.");
    [Fact] void should_not_probe_storage() => _lookups.ShouldEqual(0);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
