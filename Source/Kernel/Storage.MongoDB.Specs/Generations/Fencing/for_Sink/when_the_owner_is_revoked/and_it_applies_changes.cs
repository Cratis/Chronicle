// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.when_the_owner_is_revoked;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_applies_changes(ReplicaSetMongoDBFixture fixture) : Contract.given.an_accumulating_read_model<FencedMongoSinkHarness>
{
    FencedMongoSinkHarness _fencedHarness;
    Exception? _error;
    int _count;

    protected override FencedMongoSinkHarness CreateHarness() => _fencedHarness = new() { ConnectionString = fixture.ConnectionString };

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _fencedHarness.Revoke();
        _error = await Catch.Exception(() => _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL));
        _count = await CurrentCount();
    }

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_leave_the_persisted_state_as_it_was() => _count.ShouldEqual(1);
}
