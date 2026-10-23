// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_a_live_insert_races<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;

    async Task Because()
    {
        await Task.WhenAll(
            _sink.ReplaceIsolated(_key, State(20), 20),
            _sink.ApplyChanges(_key, ChangesetSettingCountTo(30), 30, SinkWriteMode.GuardedUpsert));
        _count = await CurrentCount();
    }

    [Fact] public void should_keep_the_newer_live_state() => _count.ShouldEqual(30);
}
