// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_round_trips_through_mongodb_storage : given.content_with_storage_conversions
{
    async Task Establish()
    {
        var sequence = new Storage.MongoDB.EventSequences.EventSequenceStorage("store", "tenant", "log", Substitute.For<IEventStoreNamespaceDatabase>(), Substitute.For<IEventConverter>(), _storage.GetEventStore("store").EventTypes, Substitute.For<IIdentityStorage>(), _converter, new JsonSerializerOptions(), NullLogger<Storage.MongoDB.EventSequences.EventSequenceStorage>.Instance);
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => sequence.SerializeContentForVerification(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));

        // This is the write pipeline in MongoDB EventSequenceStorage, including BSON encoding
        // and decoding, followed by its real read converter. No database server is necessary.
        var json = _converter.ToJsonObject(_content, _schema);
        var bson = BsonDocument.Parse(JsonSerializer.Serialize(json));
        bson = BsonSerializer.Deserialize<BsonDocument>(bson.ToBson());
        var stored = new Event(EventSequenceNumber.First, CorrelationId.New(), [], [], "event", DateTimeOffset.UtcNow, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument> { ["1"] = bson }, new Dictionary<string, string>(), []);
        var converter = new EventConverter("store", "tenant", _storage.GetEventStore("store").EventTypes, Substitute.For<IIdentityStorage>(), _converter);
        _stored = await converter.ToAppendedEvent(stored);
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_refuse_equality_after_storage_loses_decimal_precision() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
