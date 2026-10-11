// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClientManager;

public class when_the_same_servers_are_requested : Specification
{
    IMongoDBClientFactory _factory;
    MongoDBClientManager _manager;
    IMongoClient _first;
    IMongoClient _second;

    void Establish()
    {
        _factory = Substitute.For<IMongoDBClientFactory>();
        _factory.Create(Arg.Any<MongoClientSettings>()).Returns(_ => Substitute.For<IMongoClient>());
        _manager = new MongoDBClientManager(_factory);
    }

    void Because()
    {
        _first = _manager.GetClientFor(new MongoClientSettings { Servers = [new MongoServerAddress("one"), new MongoServerAddress("two")] });
        _second = _manager.GetClientFor(new MongoClientSettings { Servers = [new MongoServerAddress("two"), new MongoServerAddress("one")] });
    }

    [Fact] void should_share_the_client() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_create_only_one_client() => _factory.Received(1).Create(Arg.Any<MongoClientSettings>());
}
