// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_incrementing_concurrently;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    ObserverHandledCountEntry[] _rows;

    async Task Because()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = Enumerable.Range(0, 8).Select(async _ =>
        {
            await start.Task;
            await _harness.Storage.Increment("observer", "partition", Counts(2));
            await _harness.Storage.Increment("observer", "partition", Counts(3));
        }).ToArray();
        start.SetResult();
        await Task.WhenAll(writers);
        _rows = await _harness.Rows();
    }

    [Fact] public void should_keep_one_logical_row() => _rows.Length.ShouldEqual(1);
    [Fact] public void should_not_lose_concurrent_inserts_or_updates() => _rows.Single().Count.ShouldEqual(40L);
}
