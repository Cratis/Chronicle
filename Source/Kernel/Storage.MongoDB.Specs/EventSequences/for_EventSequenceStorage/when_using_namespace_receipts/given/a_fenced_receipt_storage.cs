// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts.given;

public class a_fenced_receipt_storage : when_appending_under_a_namespace_fence.given.a_fenced_append_storage
{
    protected readonly List<IClientSessionHandle> _receiptReadSessions = [];
    protected readonly List<IClientSessionHandle> _receiptWriteSessions = [];
    protected EffectReceipt _receipt;
    protected EventPublication _publication = new("publication", "fingerprint");
    protected BsonDocument[] _foundReceipts = [];
    protected BsonDocument _insertedReceipt;
    protected Exception _receiptFailure;
    protected Action _duringReceiptWrite;
    protected CancellationToken _receiptCancellation;

    protected void StoreReceipt() => _foundReceipts = [new BsonDocument("data", JsonSerializer.Serialize(_receipt))];

    protected void StorePublication() => _foundEvents = [new Event(
        99,
        _event.CorrelationId,
        [],
        [],
        _event.EventType.Id,
        _event.Occurred,
        _event.EventSourceType,
        _event.EventSourceId,
        _event.EventStreamType,
        _event.EventStreamId,
        [],
        new Dictionary<string, BsonDocument>(),
        new Dictionary<string, string>(),
        [],
        null)
    {
        PublicationId = _publication.Id,
        PublicationFingerprint = _publication.Fingerprint
    }];

    static IAsyncCursor<BsonDocument> ReceiptCursor(IEnumerable<BsonDocument> documents)
    {
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns(documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    void Establish()
    {
        _receipt = new("effect:test", new("occurrence", 1), EffectOutcome.Appended, EventSequenceId.Log, _event.SequenceNumber, _event.SequenceNumber, [], null, DateTimeOffset.UnixEpoch);
        _events.Database.Returns(_destination);
        var receipts = Substitute.For<IMongoCollection<BsonDocument>>();
        _destination.GetCollection<BsonDocument>(WellKnownCollectionNames.EffectReceipts).Returns(receipts);
        receipts.FindAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _steps.Add("read-receipt");
                _receiptReadSessions.Add(call.Arg<IClientSessionHandle>());
                _receiptCancellation = call.Arg<CancellationToken>();
                return Task.FromResult(ReceiptCursor(_foundReceipts));
            });
        receipts.InsertOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<BsonDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _steps.Add("insert-receipt");
                _receiptWriteSessions.Add(call.Arg<IClientSessionHandle>());
                _receiptCancellation = call.Arg<CancellationToken>();
                _insertedReceipt = call.Arg<BsonDocument>();
                _duringReceiptWrite?.Invoke();
                if (_receiptFailure is not null) throw _receiptFailure;
                return Task.CompletedTask;
            });
    }
}
