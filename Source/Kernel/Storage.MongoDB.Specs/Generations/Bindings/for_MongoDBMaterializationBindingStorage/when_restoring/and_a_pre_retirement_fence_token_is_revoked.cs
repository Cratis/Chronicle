// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.when_restoring;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_pre_retirement_fence_token_is_revoked(ReplicaSetMongoDBFixture fixture) : given.a_mongo_binding(fixture)
{
    FenceToken _token;
    long? _retiredEpoch;
    Exception _error;
    bool _writesCalled;

    async Task Establish()
    {
        _token = await new FenceGates(Database).Claim(_key.Value, MaterializationGeneration.First.Value);
        await Reserve(true);
        await _storage.TryPublish(_store, _key, MaterializationGeneration.First, new(1), _candidate, _rollbackWindow, _receipt);
        _retiredEpoch = await GateWrites.CurrentEpoch(Database, new(_key, MaterializationGeneration.First));
    }

    async Task Because()
    {
        await _storage.TryRestore(_store, _key, _candidate, new(2), MaterializationGeneration.First, _rollbackWindow, new(Guid.NewGuid()));
        var executor = new FencedBatchExecutor(new MongoFenceStore(Database));
        _error = await Catch.Exception(() => executor.Execute(_token, _ =>
        {
            _writesCalled = true;
            return Task.FromResult(1);
        }));
    }

    [Fact] void should_refuse_the_pre_retirement_token() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_run_the_stale_writes() => _writesCalled.ShouldBeFalse();
    [Fact] async Task should_advance_the_gate_again_on_restore() => (await _storage.Get(_store, _key))!.Fence.ShouldEqual(new MaterializationFence(new(_key, MaterializationGeneration.First), _retiredEpoch!.Value + 1));
    [Fact] async Task should_revoke_the_displaced_generation_again() => (await GateWrites.CurrentEpoch(Database, new(_key, _candidate))).ShouldEqual(2L);
}
