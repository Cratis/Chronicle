// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A guarded write whose event advances the watermark is applied.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_event_is_beyond_the_watermark<THarness> : given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;

    async Task Establish() =>
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _count = await CurrentCount();
    }

    [Fact] public void should_apply_the_event() => _count.ShouldEqual(2);
}
