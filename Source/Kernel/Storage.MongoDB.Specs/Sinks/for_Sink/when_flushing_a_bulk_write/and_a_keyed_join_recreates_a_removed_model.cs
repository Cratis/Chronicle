// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

[Collection(MongoDBCollection.Name)]
public class and_a_keyed_join_recreates_a_removed_model(MongoDBFixture fixture) : for_Sink.given.a_replayable_bulk_sink(fixture)
{
    ExpandoObject? _recreated;
    BsonDocument[] _documents;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, Changes(1), 1UL);
        await _sink.BeginBulk();
        var removed = Changes(0);
        removed.Remove();
        await _sink.ApplyChanges(_key, removed, 2UL);
    }

    async Task Because()
    {
        var recreated = Changes(3);
        recreated.Add(new Joined(new ExpandoObject(), "other", "id", ArrayIndexers.NoIndexers, []) { HasKeyedFrom = true });
        await _sink.ApplyChanges(_key, recreated, 3UL);
        _recreated = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
        _documents = (await (await _primary.FindAsync(FilterDefinition<BsonDocument>.Empty)).ToListAsync()).ToArray();
    }

    [Fact] void should_find_the_recreated_model_before_closure() => _recreated.ShouldNotBeNull();
    [Fact] void should_persist_the_recreated_model() => _documents.Select(document => document["count"].AsInt32).ShouldContainOnly(3);
}
