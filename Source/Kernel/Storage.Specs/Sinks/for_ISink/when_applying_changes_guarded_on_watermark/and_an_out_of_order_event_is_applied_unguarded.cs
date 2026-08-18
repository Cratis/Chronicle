// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A projection whose key collapses several event sources onto one document is written out of order on purpose,
/// so it must never be guarded. This pins that an unguarded write still lands behind the watermark.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_an_out_of_order_event_is_applied_unguarded<THarness> : given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;

    async Task Establish() =>
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 7UL);
        _count = await CurrentCount();
    }

    [Fact] public void should_apply_the_out_of_order_event() => _count.ShouldEqual(2);
}
