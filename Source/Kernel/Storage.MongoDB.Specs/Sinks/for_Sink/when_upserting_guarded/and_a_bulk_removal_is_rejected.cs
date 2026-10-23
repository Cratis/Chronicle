// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_upserting_guarded;

[Collection(MongoDBCollection.Name)]
public class and_a_bulk_removal_is_rejected(MongoDBFixture fixture) : Contract.an_isolated_read_model<MongoSinkHarness>
{
    int? _beforeFlush;
    int? _afterFlush;

    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };

    async Task Establish()
    {
        await _sink.ReplaceIsolated(_key, State(30), 30);
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        var changes = ChangesetSettingCountTo(10);
        changes.HasBeenRemoved().Returns(true);
        await _sink.ApplyChanges(_key, changes, 10, SinkWriteMode.GuardedUpsert);
        _beforeFlush = await CurrentCountOrNull();
        await _sink.FlushBulk();
        _afterFlush = await CurrentCountOrNull();
        await _sink.EndBulk();
    }

    [Fact] void should_not_answer_from_an_unconfirmed_removal() => _beforeFlush.ShouldEqual(30);
    [Fact] void should_read_the_retained_document_after_flush() => _afterFlush.ShouldEqual(30);
}
