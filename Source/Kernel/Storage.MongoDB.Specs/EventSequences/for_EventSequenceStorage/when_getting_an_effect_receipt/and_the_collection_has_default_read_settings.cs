// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_an_effect_receipt;

public class and_the_collection_has_default_read_settings : given.a_receipt_lookup
{
    async Task Because() => _result = await _storage.GetEffectReceipt(ReceiptId);

    [Fact] void should_use_majority_read_concern() => _receipts.Received(1).WithReadConcern(ReadConcern.Majority);
    [Fact] void should_use_primary_read_preference() => _majorityReceipts.Received(1).WithReadPreference(ReadPreference.Primary);
    [Fact] void should_return_no_receipt_for_a_genuine_miss() => _result.ShouldBeNull();
    [Fact] void should_dispose_the_cursor() => _cursor.Received(1).Dispose();

    [Fact]
    void should_filter_by_the_receipt_identity()
    {
        var registry = BsonSerializer.SerializerRegistry;
        var filter = _filter.Render(new RenderArgs<BsonDocument>(registry.GetSerializer<BsonDocument>(), registry));
        filter.ShouldEqual(new BsonDocument("_id", ReceiptId));
    }
}
