// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_replaying_an_effect : given.a_fenced_receipt_storage
{
    EffectReceiptWriteResult _outcome;

    void Establish() => StoreReceipt();
    async Task Because() => _outcome = await _storage.AppendManyWithEffectReceipt(_context, [], _receipt);

    [Fact] void should_settle_before_validating_the_replayed_batch() => _outcome.ShouldEqual(EffectReceiptWriteResult.AlreadyRecorded);
    [Fact] void should_commit_the_fence_even_for_replay() => _steps.SequenceEqual(["fence", "read-receipt", "commit"]).ShouldBeTrue();
    [Fact] void should_write_no_receipt() => _receiptWriteSessions.ShouldBeEmpty();
    [Fact] void should_write_no_events() => _writeSessions.ShouldBeEmpty();
}
