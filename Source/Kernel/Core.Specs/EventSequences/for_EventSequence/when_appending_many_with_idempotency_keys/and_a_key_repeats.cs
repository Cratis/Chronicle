// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_a_key_repeats : given.an_idempotent_batch
{
    void Establish() => _batchKeys[2] = _batchKeys[0];
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_whole_batch() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_repeated_key() => _batchResult.Result.Errors.Single().Value.ShouldEqual("IdempotencyKeyRepeatedInBatch: Idempotency key 'one' occurs more than once in this batch. Nothing was written.");
    [Fact] void should_not_probe_storage() => _lookups.ShouldEqual(0);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
