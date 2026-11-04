// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_an_effect_receipt.given;

public class a_receipt_lookup : for_EventSequenceStorage.given.an_event_sequence_storage
{
    protected const string ReceiptId = "effect:one";
    protected IMongoCollection<BsonDocument> _receipts;
    protected IMongoCollection<BsonDocument> _majorityReceipts;
    protected IMongoCollection<BsonDocument> _primaryReceipts;
    protected IAsyncCursor<BsonDocument> _cursor;
    protected FilterDefinition<BsonDocument> _filter;
    protected EffectReceipt? _result;

    void Establish()
    {
        var database = Substitute.For<IMongoDatabase>();
        _collection.Database.Returns(database);
        _receipts = Substitute.For<IMongoCollection<BsonDocument>>();
        _majorityReceipts = Substitute.For<IMongoCollection<BsonDocument>>();
        _primaryReceipts = Substitute.For<IMongoCollection<BsonDocument>>();
        database.GetCollection<BsonDocument>("effect-receipts").Returns(_receipts);
        _receipts.WithReadConcern(ReadConcern.Majority).Returns(_majorityReceipts);
        _majorityReceipts.WithReadPreference(ReadPreference.Primary).Returns(_primaryReceipts);
        _cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        _cursor.Current.Returns([]);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _primaryReceipts.FindAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(filter => _filter = filter),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>()).Returns(_cursor);
    }
}
