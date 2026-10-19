// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_one_key_is_unverifiable : given.an_idempotent_batch
{
    async Task Establish()
    {
        await ProtectBatch();
        await _backingKeys.DeleteFor(EventStore, EventStoreNamespace, _eventSourceId.Value);
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_whole_batch() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_unverifiable_key() => _batchResult.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Unverifiable);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
