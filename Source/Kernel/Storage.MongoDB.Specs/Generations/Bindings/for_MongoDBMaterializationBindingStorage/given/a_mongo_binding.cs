// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.given;

public class a_mongo_binding(ReplicaSetMongoDBFixture fixture) : Contract.a_bound_materialization<MongoMaterializationBindingStorageHarness>
{
    protected MongoMaterializationBindingStorageHarness _mongo;
    protected IMongoDatabase Database => _mongo.Database(_store, _key.Namespace);
    protected virtual IGenerationGateWrites GateWrites => new MongoDBGenerationGateWrites();
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => _mongo = new() { ConnectionString = fixture.ConnectionString, GateWrites = GateWrites };
}
