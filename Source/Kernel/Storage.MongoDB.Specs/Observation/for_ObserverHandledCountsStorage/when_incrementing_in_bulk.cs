// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage;

public class when_incrementing_in_bulk : given.an_observer_handled_counts_storage
{
    UpdateOneModel<ObserverPartitionCounts>[] _models;
    BulkWriteOptions _options;

    void Establish() => _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
        .Returns(call =>
        {
            _models = call.Arg<IEnumerable<WriteModel<ObserverPartitionCounts>>>().Cast<UpdateOneModel<ObserverPartitionCounts>>().ToArray();
            _options = call.Arg<BulkWriteOptions>();
            return Task.FromResult<BulkWriteResult<ObserverPartitionCounts>>(new BulkWriteResult<ObserverPartitionCounts>.Acknowledged(_models.Length, 0, 0, 0, 0, _models, []));
        });

    async Task Because() => await _storage.Increment(_observerId,
    [
        new((Key)"first", new Dictionary<EventTypeId, EventCount> { ["a"] = 2UL, ["b"] = 1UL }),
        new((Key)"empty", new Dictionary<EventTypeId, EventCount>()),
        new((Key)"second", new Dictionary<EventTypeId, EventCount> { ["a"] = 3UL })
    ]);

    [Fact] void should_use_one_round_trip() => _collection.Received(1).BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_skip_the_empty_partition() => _models.Length.ShouldEqual(2);
    [Fact] void should_keep_the_write_order() => _options.IsOrdered.ShouldBeTrue();
    [Fact] void should_upsert_every_partition() => _models.All(_ => _.IsUpsert).ShouldBeTrue();
    [Fact] void should_increment_each_type_atomically() => RenderUpdate(_models[0])["$inc"].AsBsonDocument.ShouldEqual(new BsonDocument { ["counts.a"] = 2L, ["counts.b"] = 1L });
    [Fact] void should_keep_partition_counts_separate() => RenderUpdate(_models[1])["$inc"].AsBsonDocument.ShouldEqual(new BsonDocument("counts.a", 3L));
    [Fact] void should_use_distinct_partition_filters() => _models[0].Filter.Render(RenderArguments()).Equals(_models[1].Filter.Render(RenderArguments())).ShouldBeFalse();

    static BsonDocument RenderUpdate(UpdateOneModel<ObserverPartitionCounts> model) => model.Update.Render(RenderArguments()).AsBsonDocument;

    static RenderArgs<ObserverPartitionCounts> RenderArguments() => new(BsonSerializer.LookupSerializer<ObserverPartitionCounts>(), BsonSerializer.SerializerRegistry);
}
