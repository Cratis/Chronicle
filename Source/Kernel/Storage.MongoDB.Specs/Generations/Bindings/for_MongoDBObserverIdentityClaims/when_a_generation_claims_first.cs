// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBObserverIdentityClaims;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_generation_claims_first(ReplicaSetMongoDBFixture fixture) : Contract.when_a_generation_claims_first<MongoMaterializationBindingStorageHarness>
{
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
