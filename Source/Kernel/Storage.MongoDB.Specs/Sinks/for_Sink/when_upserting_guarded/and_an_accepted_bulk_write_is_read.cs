// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_upserting_guarded;

[Collection(MongoDBCollection.Name)]
public class and_an_accepted_bulk_write_is_read(MongoDBFixture fixture) : Contract.an_isolated_read_model<MongoSinkHarness>
{
    int? _count;

    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };

    async Task Establish() => await _sink.BeginBulk();

    async Task Because()
    {
        var changes = ChangesetSettingCountTo(10);
        changes.CurrentState.Returns(State(10));
        await _sink.ApplyChanges(_key, changes, 10, SinkWriteMode.GuardedUpsert);
        _count = await CurrentCountOrNull();
        await _sink.EndBulk();
    }

    [Fact] void should_read_the_accepted_write_before_computing_the_next_changeset() => _count.ShouldEqual(10);
}
