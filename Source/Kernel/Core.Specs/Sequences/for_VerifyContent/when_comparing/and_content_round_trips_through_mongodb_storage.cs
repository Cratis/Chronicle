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
using MongoDB.Driver;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_round_trips_through_mongodb_storage : given.content_with_storage_conversions
{
    async Task Establish()
    {
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        var collection = Substitute.For<IMongoCollection<Event>>();
        database.GetEventSequenceCollectionFor("log").Returns(collection);
        Event inserted = null!;
        collection.When(current => current.InsertOneAsync(Arg.Any<Event>(), Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>())).Do(call => inserted = call.Arg<Event>());
        var sequence = new Storage.MongoDB.EventSequences.EventSequenceStorage("store", "tenant", "log", database, Substitute.For<IEventConverter>(), _storage.GetEventStore("store").EventTypes, Substitute.For<IIdentityStorage>(), _converter, new JsonSerializerOptions(), NullLogger<Storage.MongoDB.EventSequences.EventSequenceStorage>.Instance);
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => sequence.SerializeContentForVerification(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));

        // Exercise the real schema-aware write pipeline, driver encoding and read converter.
        // A substitute collection captures the document, so no database server is necessary.
        var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, _generations, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
        inserted.Content["1"] = BsonSerializer.Deserialize<BsonDocument>(inserted.Content["1"].ToBson());
        var converter = new EventConverter("store", "tenant", _storage.GetEventStore("store").EventTypes, Substitute.For<IIdentityStorage>(), _converter);
        _stored = await converter.ToAppendedEvent(inserted);
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_verify_the_exact_stored_decimal_content() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
