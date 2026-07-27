// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// Catch-up runs the sink in bulk mode, where reads are answered from the pending-state cache. A guarded write
/// the server would reject must therefore not seed that cache either, or the next event for the same key reads
/// the doubled state and persists it.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_redelivery_happens_inside_a_bulk_window(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    int _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        await _sink.EndBulk();
        _count = await CurrentCount();
    }

    [Fact] void should_not_apply_the_redelivered_event() => _count.ShouldEqual(1);
}
