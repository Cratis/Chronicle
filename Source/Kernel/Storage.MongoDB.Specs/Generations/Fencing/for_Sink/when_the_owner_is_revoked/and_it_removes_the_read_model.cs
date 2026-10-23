// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.when_the_owner_is_revoked;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_removes_the_read_model(ReplicaSetMongoDBFixture fixture) : Contract.given.an_accumulating_read_model<FencedMongoSinkHarness>
{
    FencedMongoSinkHarness _fencedHarness;
    Exception? _error;
    int? _count;

    protected override FencedMongoSinkHarness CreateHarness() => _fencedHarness = new() { ConnectionString = fixture.ConnectionString };

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _fencedHarness.Revoke();
        var removal = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        removal.InitialState.Returns(new ExpandoObject());
        removal.HasBeenRemoved().Returns(true);
        _error = await Catch.Exception(() => _sink.ApplyChanges(_key, removal, 43UL));
        _count = await CurrentCountOrNull();
    }

    [Fact] void should_refuse_the_removal() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_keep_the_read_model() => _count.ShouldEqual(1);
}
