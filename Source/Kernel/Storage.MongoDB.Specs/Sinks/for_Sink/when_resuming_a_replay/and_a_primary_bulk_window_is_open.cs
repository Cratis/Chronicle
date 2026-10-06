// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_a_primary_bulk_window_is_open(MongoDBFixture fixture) : given.a_replayable_bulk_sink(fixture)
{
    ExpandoObject? _replayState;
    BsonDocument[] _primaryDocuments;
    BsonDocument[] _replayDocuments;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await _sink.ApplyChanges(_key, Changes(9), 9UL);
    }

    async Task Because()
    {
        await _sink.ResumeReplay(_context);
        _replayState = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
        _primaryDocuments = (await (await _primary.FindAsync(FilterDefinition<BsonDocument>.Empty)).ToListAsync()).ToArray();
        _replayDocuments = (await (await _replay.FindAsync(FilterDefinition<BsonDocument>.Empty)).ToListAsync()).ToArray();
    }

    [Fact] void should_start_replay_without_the_primary_cached_state() => _replayState.ShouldBeNull();
    [Fact] void should_flush_the_accepted_operation_to_primary() => _primaryDocuments.Select(document => document["count"].AsInt32).ShouldContainOnly(9);
    [Fact] void should_not_send_the_primary_operation_to_replay() => _replayDocuments.ShouldBeEmpty();
}
