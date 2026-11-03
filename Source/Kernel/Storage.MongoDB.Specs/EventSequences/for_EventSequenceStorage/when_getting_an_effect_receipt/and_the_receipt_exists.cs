// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_an_effect_receipt;

public class and_the_receipt_exists : given.a_receipt_lookup
{
    EffectReceipt _receipt;

    void Establish()
    {
        _receipt = new(ReceiptId, new("occurrence", 1), EffectOutcome.NoEvents, null, null, null, [], "{\"winner\":1}", DateTimeOffset.UnixEpoch);
        _cursor.Current.Returns([new BsonDocument { { "_id", ReceiptId }, { "data", JsonSerializer.Serialize(_receipt) } }]);
    }

    async Task Because() => _result = await _storage.GetEffectReceipt(ReceiptId);

    [Fact] void should_find_the_receipt() => _result.ShouldNotBeNull();
    [Fact] void should_preserve_the_receipt_identity() => _result!.Id.ShouldEqual(_receipt.Id);
    [Fact] void should_preserve_the_lease() => _result!.Lease.ShouldEqual(_receipt.Lease);
    [Fact] void should_preserve_the_outcome() => _result!.Outcome.ShouldEqual(_receipt.Outcome);
    [Fact] void should_preserve_the_winning_response() => _result!.Response.ShouldEqual(_receipt.Response);
    [Fact] void should_preserve_the_recording_time() => _result!.RecordedAt.ShouldEqual(_receipt.RecordedAt);
}
