// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

[Collection(MongoDBCollection.Name)]
public class and_the_event_is_beyond_the_watermark(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    int _count;

    async Task Establish() =>
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _count = await CurrentCount();
    }

    [Fact] void should_apply_the_event() => _count.ShouldEqual(2);
}
