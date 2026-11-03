// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.given;

public abstract class a_counts_storage(CountsHarness harness) : Specification
{
    protected readonly CountsHarness _harness = harness;
    protected static IReadOnlyDictionary<EventTypeId, EventCount> Counts(ulong count = 1) => new Dictionary<EventTypeId, EventCount> { ["type"] = count };
    protected static KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>[] Cells(int count) => Enumerable.Range(0, count)
        .Select(index => new KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>((Key)$"partition-{index:D4}", Counts())).ToArray();

    public static CountsHarness Sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-counts-{Guid.NewGuid():N}.db");
        return new("sqlite", () => Task.FromResult($"DataSource={path};Pooling=False;Default Timeout=30"), _ =>
        {
            File.Delete(path);
            return Task.CompletedTask;
        });
    }

    async Task Establish() => await _harness.Initialize();
    async Task Destroy() => await _harness.DisposeAsync();
}
