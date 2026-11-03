// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_an_effect_receipt;

public class and_the_lookup_fails : given.a_receipt_lookup
{
    Exception _failure;
    Exception? _error;

    void Establish()
    {
        _failure = new MongoException("Receipt read failed");
        _primaryReceipts.FindAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncCursor<BsonDocument>>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => _storage.GetEffectReceipt(ReceiptId));

    [Fact] void should_propagate_the_lookup_failure_instead_of_returning_a_miss() => _error.ShouldBeSame(_failure);
}
