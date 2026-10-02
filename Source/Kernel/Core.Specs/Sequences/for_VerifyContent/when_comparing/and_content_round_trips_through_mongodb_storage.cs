// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_round_trips_through_mongodb_storage : given.content_with_storage_conversions
{
    async Task Establish()
    {
        // This is the write pipeline in MongoDB EventSequenceStorage, including BSON encoding
        // and decoding, followed by its real read converter. No database server is necessary.
        var json = _converter.ToJsonObject(_content, _schema);
        var bson = BsonDocument.Parse(JsonSerializer.Serialize(json));
        bson = BsonSerializer.Deserialize<BsonDocument>(bson.ToBson());
        var stored = new Event(EventSequenceNumber.First, CorrelationId.New(), [], [], "event", DateTimeOffset.UtcNow, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument> { ["1"] = bson }, new Dictionary<string, string>(), []);
        var converter = new EventConverter("store", "tenant", _storage.GetEventStore("store").EventTypes, Substitute.For<IIdentityStorage>(), _converter);
        _stored = await converter.ToAppendedEvent(stored);
    }

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_recognize_the_duplicate_after_storage_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
