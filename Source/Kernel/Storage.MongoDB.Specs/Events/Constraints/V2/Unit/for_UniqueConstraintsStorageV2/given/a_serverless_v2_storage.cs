// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2.given;

public class a_serverless_v2_storage : Specification
{
    protected IMongoDatabase _database = null!;
    protected UniqueConstraintsStorageV2 _storage = null!;
    protected readonly UniqueConstraintDefinition _definition = new("constraint", []);
    protected readonly UniqueConstraintStorageScope _scope = new(new string('0', 32));
    protected readonly UniqueConstraintValue _value = new(new string('a', 64));
    protected readonly UniqueConstraintWriteAuthority _authority = new(new(Guid.NewGuid()), UniqueConstraintWritePhase.Live, new(0));

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _database.WithReadPreference(Arg.Any<ReadPreference>()).Returns(_database);
        _database.WithReadConcern(Arg.Any<ReadConcern>()).Returns(_database);
        _database.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(_database);
        _storage = new(_database, "sequence");
        _database.ClearReceivedCalls();
    }
}
