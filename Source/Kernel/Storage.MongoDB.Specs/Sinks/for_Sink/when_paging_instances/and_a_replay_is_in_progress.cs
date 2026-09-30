// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_paging_instances;

/// <summary>
/// While a replay rebuilds its own collection, a page and its count both come from the primary collection, as they do when observing.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the container.</param>
/// <remarks>
/// The replay collection is written to directly: writes made through the sink during a replay are only buffered in
/// its bulk window, which would leave the replay collection empty and prove nothing about which collection is read.
/// </remarks>
[Collection(MongoDBCollection.Name)]
public class and_a_replay_is_in_progress(MongoDBFixture fixture) : a_populated_sink<MongoSinkHarness>
{
    MongoSinkHarness _harness;
    ReadModelInstances _page;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { Fixture = fixture };

    async Task Establish()
    {
        await _sink.BeginReplay(new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            "paged_read_models",
            "paged_read_models-revert",
            DateTimeOffset.UtcNow));
        await _harness.Database.GetCollection<BsonDocument>("replay-paged_read_models").InsertManyAsync(
        [
            new BsonDocument { { "_id", "replayed-1" }, { "name", "replayed-1" } },
            new BsonDocument { { "_id", "replayed-2" }, { "name", "replayed-2" } }
        ]);
    }

    async Task Because() => _page = await _sink.GetInstances(take: 10);

    [Fact] void should_return_the_instances_of_the_primary_collection() => Names(_page).ShouldEqual(["a", "b", "c", "d", "e"]);
    [Fact] void should_count_the_instances_of_the_primary_collection() => _page.TotalCount.ShouldEqual(5L);
}
