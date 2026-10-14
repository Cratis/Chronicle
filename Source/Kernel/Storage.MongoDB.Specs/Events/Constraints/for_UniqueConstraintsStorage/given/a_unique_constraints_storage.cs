// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.given;

public abstract class a_unique_constraints_storage(MongoDBFixture fixture) : Specification
{
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;
    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        var database = _client.GetDatabase(_databaseName);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<UniqueConstraintIndex>(Arg.Any<string>()).Returns(call => database.GetCollection<UniqueConstraintIndex>(call.ArgAt<string>(0)));
        namespaceDatabase.GetCollection<UniqueConstraintValueIndex>(Arg.Any<string>()).Returns(call => database.GetCollection<UniqueConstraintValueIndex>(call.ArgAt<string>(0)));
        _definition = new("unique", []);
        _storage = new(namespaceDatabase, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
    }

    Task Destroy() => _client.DropDatabaseAsync(_databaseName);
}
