// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_a_write_is_buffered<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(99), 19);
    }

    async Task Because()
    {
        await _sink.ReplaceIsolated(_key, State(20), 20);
        await _sink.EndBulk();
        _count = await CurrentCount();
    }

    [Fact] public void should_not_overwrite_the_rebuilt_state() => _count.ShouldEqual(20);
}
