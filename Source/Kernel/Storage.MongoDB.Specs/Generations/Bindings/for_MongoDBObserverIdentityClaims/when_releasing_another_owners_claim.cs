// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBObserverIdentityClaims;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_releasing_another_owners_claim(ReplicaSetMongoDBFixture fixture) : Contract.when_releasing_another_owners_claim<MongoMaterializationBindingStorageHarness>
{
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
