// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_an_event_has_multiple_generations(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    AppendedEvent _acknowledged;
    Event _stored;
    ExpandoObject _original;

    async Task Because()
    {
        _original = new ExpandoObject();
        var upcast = new ExpandoObject();
        _expandoObjectConverter.ToJsonObject(upcast, Arg.Any<JsonSchema>()).Returns(new JsonObject { ["value"] = "migrated" });
        var @event = EventAt(EventSequenceNumber.First) with
        {
            Content = new ExpandoObject(),
            Hash = "stale-hash",
            GenerationalContent = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = _original, [new EventTypeGeneration(2)] = upcast },
            ContentHashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "original-hash", [new EventTypeGeneration(2)] = "migrated-hash" }
        };
        _acknowledged = (await _storage.AppendMany([@event])).AsT0.Single();
        _stored = await _collection.Find(FilterDefinition<Event>.Empty).SingleAsync();
    }

    [Fact] void should_store_both_generations() => _stored.Content.Keys.ShouldContainOnly(["1", "2"]);
    [Fact] void should_store_the_upcast_content() => _stored.Content["2"]["value"].AsString.ShouldEqual("migrated");
    [Fact] void should_store_the_upcast_hash() => _stored.ContentHashes["2"].ShouldEqual("migrated-hash");
    [Fact] void should_acknowledge_both_generations() => _acknowledged.GenerationalContent.Keys.ShouldContainOnly([1, 2]);
    [Fact] void should_acknowledge_the_appended_generations_content() => _acknowledged.Content.ShouldEqual(_original);
    [Fact] void should_acknowledge_the_appended_generations_hash() => _acknowledged.Context.Hash.ShouldEqual((EventHash)"original-hash");
}
