// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.when_setting_the_binding_epoch;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.when_setting_the_binding_epoch;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_is_repeated(ReplicaSetMongoDBFixture fixture) : Contract.and_it_is_repeated<MongoMaterializationBindingStorageHarness>
{
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
