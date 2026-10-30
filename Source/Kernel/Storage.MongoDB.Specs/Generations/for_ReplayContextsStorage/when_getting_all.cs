// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;
using MongoReplayContext = Cratis.Chronicle.Storage.MongoDB.ReadModels.ReplayContext;
using ReplayContext = Cratis.Chronicle.Storage.ReadModels.ReplayContext;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_ReplayContextsStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_getting_all(ReplicaSetMongoDBFixture fixture) : Specification
{
    readonly string _name = MongoDBSpecDatabaseNames.New();
    MongoClient _client;
    ReadModels.ReplayContextsStorage _contexts;
    IReadOnlyList<ReplayContext> _actual;
    readonly ReplayContext[] _expected = [new(new("orders", ReadModelGeneration.First), "orders", "orders-revert", DateTimeOffset.FromUnixTimeSeconds(10000)), new(new("items", ReadModelGeneration.First), "items", "items-revert", DateTimeOffset.FromUnixTimeSeconds(20000))];

    async Task Establish()
    {
        _client = new(fixture.ConnectionString);
        var database = _client.GetDatabase(_name);
        var ns = Substitute.For<IEventStoreNamespaceDatabase>();
        ns.GetCollection<MongoReplayContext>(Arg.Any<string>()).Returns(call => database.GetCollection<MongoReplayContext>(call.Arg<string>()));
        _contexts = new(ns);
        foreach (var context in _expected)
        {
            await _contexts.Save(context);
        }
    }

    async Task Because() => _actual = await _contexts.GetAll();

    [Fact] void should_read_all_durable_sessions() => _actual.OrderBy(context => context.Type.Identifier.Value).ShouldEqual(_expected.OrderBy(context => context.Type.Identifier.Value));
    [Fact] async Task should_not_remove_sessions() => (await _contexts.GetAll()).Count.ShouldEqual(2);

    Task Destroy() => _client.DropDatabaseAsync(_name);
}
