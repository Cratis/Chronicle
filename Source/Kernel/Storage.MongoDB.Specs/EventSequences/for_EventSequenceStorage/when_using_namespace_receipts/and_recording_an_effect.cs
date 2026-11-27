// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_recording_an_effect : given.a_fenced_receipt_storage
{
    EffectReceiptWriteResult _outcome;

    async Task Because() => _outcome = await _storage.AppendManyWithEffectReceipt(_context, [_event], _receipt);

    [Fact] void should_record_the_effect() => _outcome.ShouldEqual(EffectReceiptWriteResult.Written);
    [Fact] void should_fence_before_receipt_and_event_work() => _steps.SequenceEqual(["fence", "read-receipt", "insert-receipt", "insert", "commit"]).ShouldBeTrue();
    [Fact] void should_read_on_the_fenced_session() => _receiptReadSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_write_the_receipt_on_the_fenced_session() => _receiptWriteSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_write_events_on_the_same_session() => _writeSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_observe_activation_cancellation() => _receiptCancellation.ShouldEqual(_activation.Token);
    [Fact] void should_preserve_the_receipt_identity() => _insertedReceipt["_id"].AsString.ShouldEqual(_receipt.Id);
    [Fact] void should_not_persist_optional_publication_identity() => _inserted.Single().PublicationId.ShouldBeNull();
}
