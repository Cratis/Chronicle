// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_mongodb_round_trip : a_storage_round_trip
{
    protected async Task StoreInMongoDB(string schemaJson, string content)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        _command = _command with { Content = content };
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        var sequence = new Storage.MongoDB.EventSequences.EventSequenceStorage("store", "tenant", "log", Substitute.For<IEventStoreNamespaceDatabase>(), Substitute.For<IEventConverter>(), eventTypes, Substitute.For<IIdentityStorage>(), _converter, new JsonSerializerOptions(), NullLogger<Storage.MongoDB.EventSequences.EventSequenceStorage>.Instance);
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => sequence.SerializeContentForVerification(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        var expando = _converter.ToExpandoObject(JsonNode.Parse(content)!.AsObject(), schema);
        var bson = BsonDocument.Parse(sequence.SerializeContentForVerification(expando, schema));
        bson = BsonSerializer.Deserialize<BsonDocument>(bson.ToBson());
        var stored = new Event(EventSequenceNumber.First, CorrelationId.New(), [], [], "event", DateTimeOffset.UtcNow, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument> { ["1"] = bson }, new Dictionary<string, string>(), []);
        _stored = await new EventConverter("store", "tenant", eventTypes, Substitute.For<IIdentityStorage>(), _converter).ToAppendedEvent(stored);
    }
}
