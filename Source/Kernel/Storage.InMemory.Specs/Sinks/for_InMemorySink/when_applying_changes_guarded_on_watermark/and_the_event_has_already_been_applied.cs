// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes_guarded_on_watermark;

public class and_the_event_has_already_been_applied : given.an_accumulating_read_model
{
    int _count;

    async Task Establish() =>
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _count = await CurrentCount();
    }

    [Fact] void should_not_apply_the_redelivered_event() => _count.ShouldEqual(1);
}
