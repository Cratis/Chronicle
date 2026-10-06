// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_the_same_replay_window_is_open(MongoDBFixture fixture) : given.a_replayable_bulk_sink(fixture)
{
    ExpandoObject? _state;
    long _documentsBeforeClosure;
    BsonDocument[] _documents;

    async Task Establish()
    {
        await _sink.BeginReplay(_context);
        await _sink.ApplyChanges(_key, Changes(1), 1UL);
    }

    async Task Because()
    {
        await _sink.ResumeReplay(_context);
        _state = await _sink.FindOrDefault(_key);
        _documentsBeforeClosure = await _replay.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await _sink.EndBulk();
        _documents = (await (await _replay.FindAsync(FilterDefinition<BsonDocument>.Empty)).ToListAsync()).ToArray();
    }

    [Fact] void should_preserve_the_cached_replay_state() => _state.ShouldNotBeNull();
    [Fact] void should_preserve_the_open_window_without_flushing() => _documentsBeforeClosure.ShouldEqual(0);
    [Fact] void should_write_the_pending_replay_operation_on_closure() => _documents.Select(document => document["count"].AsInt32).ShouldContainOnly(1);
}
