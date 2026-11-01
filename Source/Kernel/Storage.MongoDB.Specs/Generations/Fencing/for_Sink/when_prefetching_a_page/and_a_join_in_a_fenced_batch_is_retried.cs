// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.when_prefetching_a_page;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_join_in_a_fenced_batch_is_retried(ReplicaSetMongoDBFixture fixture) : given.a_prefetched_fenced_sink(fixture)
{
    int _attempts;
    int _countInsideAbortedBatch;
    int _countBeforeRetry;
    int _countAfterRetry;
    long _receiptsAfterRetry;
    BsonDocument _final;

    async Task Establish() => await _models.UpdateOneAsync(new BsonDocument("_id", "k"), Builders<BsonDocument>.Update.Set("count", 11));

    async Task Because()
    {
        await _writes.InBatch(async () =>
        {
            _attempts++;
            if (_attempts == 2) _countBeforeRetry = (await StoredDocument())["count"].ToInt32();
            await Join();
            if (_attempts == 1)
            {
                // Sink reads are outside the transaction: clearing the page exposes 11, not its prefetched 7.
                _countInsideAbortedBatch = Count((await _sink.FindOrDefault(_root))!);
                var error = new MongoException("Retry the fenced batch after its direct mutation");
                error.AddErrorLabel(FenceRetryPolicy.TransientTransactionError);
                throw error;
            }
            return 0;
        });
        var state = (await _sink.FindOrDefault(_root))!;
        _countAfterRetry = Count(state);
        _receiptsAfterRetry = await _database.GetCollection<BsonDocument>(FenceCollections.Receipts).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var next = Changes(state);
        next.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => _countAfterRetry + 1)], ArrayIndexers.NoIndexers);
        await _sink.ApplyChanges(_root, next, 2UL);
        await _sink.EndBulk();
        _final = await StoredDocument();
    }

    [Fact] void should_clear_the_prefetched_page_inside_the_fenced_batch() => _countInsideAbortedBatch.ShouldEqual(11);
    [Fact] void should_retry_the_aborted_batch_once() => _attempts.ShouldEqual(2);
    [Fact] void should_roll_back_the_direct_mutation_before_retrying() => _countBeforeRetry.ShouldEqual(11);
    [Fact] void should_commit_only_the_retried_batch() => _receiptsAfterRetry.ShouldEqual(1L);
    [Fact] void should_read_the_committed_join_instead_of_the_prefetched_document() => _countAfterRetry.ShouldEqual(42);
    [Fact] void should_apply_the_next_event_to_the_joined_state() => _final["count"].ToInt32().ShouldEqual(43);
    [Fact] void should_preserve_the_join_key() => _final["joinKey"].AsString.ShouldEqual("join");

    Task Join()
    {
        var joined = Changes(new ExpandoObject());
        joined.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => 42)], ArrayIndexers.NoIndexers);
        var join = Changes(new ExpandoObject());
        join.Add(new Joined(new ExpandoObject(), "join", new("joinKey"), ArrayIndexers.NoIndexers, joined.Changes));
        return _sink.ApplyChanges(new Key("join", ArrayIndexers.NoIndexers), join, 1UL);
    }
}
