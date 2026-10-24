// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reserving_with_a_candidate_in_flight(ReplicaSetMongoDBFixture fixture) : Contract.when_reserving_with_a_candidate_in_flight<MongoMaterializationBindingStorageHarness>
{
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
