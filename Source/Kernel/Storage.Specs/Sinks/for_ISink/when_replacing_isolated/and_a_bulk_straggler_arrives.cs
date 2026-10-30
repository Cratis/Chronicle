// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_a_bulk_straggler_arrives<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;
    IEnumerable<FailedPartition> _failures;

    async Task Establish()
    {
        await _sink.ReplaceIsolated(_key, State(20), 20);
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(99), 19, SinkWriteMode.GuardedUpsert);
        _failures = await _sink.EndBulk();
        _count = await CurrentCount();
    }

    [Fact] public void should_preserve_the_rebuilt_state() => _count.ShouldEqual(20);
    [Fact] public void should_not_fail_the_partition() => _failures.ShouldBeEmpty();
}
