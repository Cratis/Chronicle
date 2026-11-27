// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_recovering_a_receipt_collision : given.a_fenced_receipt_storage
{
    EffectReceiptWriteResult _outcome;

    void Establish()
    {
        _receiptFailure = Duplicate();
        _duringReceiptWrite = StoreReceipt;
    }

    async Task Because() => _outcome = await _storage.AppendManyWithEffectReceipt(_context, [_event], _receipt);

    [Fact] void should_return_the_durable_receipt() => _outcome.ShouldEqual(EffectReceiptWriteResult.AlreadyRecorded);
    [Fact] void should_refence_before_recovery() => _steps.SequenceEqual(["fence", "read-receipt", "insert-receipt", "abort", "fence", "read-receipt", "commit"]).ShouldBeTrue();
    [Fact] void should_lookup_on_the_recovery_session() => _receiptReadSessions[^1].ShouldEqual(_sessions[1]);
    [Fact] void should_not_append_events() => _writeSessions.ShouldBeEmpty();
}
