// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_generations_are_disabled(ReplicaSetMongoDBFixture fixture) : Contract.given.an_accumulating_read_model<MongoSinkHarness>
{
    Exception? _error;
    int? _count;

    protected override MongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        var removal = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        removal.InitialState.Returns(new ExpandoObject());
        removal.HasBeenRemoved().Returns(true);
        _error = await Catch.Exception(() => _sink.ApplyChanges(_key, removal, 43UL));
        _count = await CurrentCountOrNull();
    }

    [Fact] void should_allow_the_current_owner_to_remove() => _error.ShouldBeNull();
    [Fact] void should_delete_the_read_model() => _count.ShouldBeNull();
}
