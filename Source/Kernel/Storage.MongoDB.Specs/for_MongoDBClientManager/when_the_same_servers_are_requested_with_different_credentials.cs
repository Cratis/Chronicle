// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClientManager;

public class when_the_same_servers_are_requested_with_different_credentials : Specification
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
        _first = _manager.GetClientFor(new MongoClientSettings { Server = new MongoServerAddress("shared"), Credential = MongoCredential.CreateCredential("admin", "tenant-a", "a") });
        _second = _manager.GetClientFor(new MongoClientSettings { Server = new MongoServerAddress("shared"), Credential = MongoCredential.CreateCredential("admin", "tenant-b", "b") });
    }

    [Fact] void should_not_share_a_client() => ReferenceEquals(_first, _second).ShouldBeFalse();
}
