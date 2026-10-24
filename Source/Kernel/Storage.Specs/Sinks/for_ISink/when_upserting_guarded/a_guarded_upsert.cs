// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_upserting_guarded;

public abstract class a_guarded_upsert<THarness>(ulong? storedSequence, int expectedCount) : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;
    IEnumerable<FailedPartition> _failures;

    async Task Establish()
    {
        if (storedSequence is { } sequence)
        {
            await Seed(sequence);
        }
    }

    async Task Because()
    {
        _failures = await _sink.ApplyChanges(_key, ChangesetSettingCountTo(20), 20, SinkWriteMode.GuardedUpsert);
        _count = await CurrentCount();
    }

    [Fact] public void should_store_only_an_admitted_write() => _count.ShouldEqual(expectedCount);
    [Fact] public void should_not_fail_the_partition() => _failures.ShouldBeEmpty();
}
