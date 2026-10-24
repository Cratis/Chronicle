// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_recreating_build_storage(ReplicaSetMongoDBFixture fixture) : Contract.a_build_storage<MongoMaterializationBindingStorageHarness>
{
    MongoMaterializationBindingStorageHarness _mongoHarness;
    GenerationReservation _original;
    GenerationReservation _retry;
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => _mongoHarness = new() { ConnectionString = fixture.ConnectionString };
    async Task Establish() => _original = await Build();
    async Task Because()
    {
        _builds = (IGenerationBuildStorage)_mongoHarness.Create(_time);
        _retry = await Build();
    }
    [Fact] void should_recover_the_committed_operation() => _retry.ShouldEqual(_original);
    [Fact] async Task should_recover_the_execution() => (await _builds.GetExecution(_store, _key, _original.Record!.Identity.Generation)).ShouldEqual(_execution);
    [Fact] async Task should_recover_the_observer_claim() => (await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, _original.Record!.Identity.ObserverId))!.Revision.ShouldEqual(_execution.Revision);
}
