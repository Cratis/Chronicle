// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_two_graph_targets_share_an_inventory_id : given.a_graph_database
{
    readonly TestClock _clock = new();
    SinkProvenanceCache _cache;
    SinkProvenanceLease _first;
    bool _marked;
    Exception? _failure;

    async Task Establish()
    {
        var inventory = Inventory(Guid.NewGuid(), SinkProvenanceState.CurrentInventoryVersion);
        var second = Substitute.For<IAsyncQueryRunner>();
        _runtime.RunAsync(Read).Returns(_ => Cursor(inventory));
        second.RunAsync(Read).Returns(_ => _marked ? Cursor(inventory, Protection()) : Cursor(inventory));

        _cache = new(_clock);
        _first = await _cache.Bind("first", () => _provenance).GetLease();
        await _first.Ensure();
        var secondStorage = StorageFor(second, Substitute.For<IAsyncQueryRunner>(), "second-physical");
        await _cache.Bind("second", () => secondStorage).GetLease();
        _marked = true;
        _clock.Timestamp = TimeSpan.FromSeconds(2).Ticks;
    }

    async Task Because() => _failure = await Catch.Exception(_first.Ensure);

    [Fact] void should_deny_the_previously_admitted_graph() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_revoke_the_previously_admitted_graphs_lease() => _first.Revoked.IsCancellationRequested.ShouldBeTrue();

    async Task Destroy() => await _cache.DisposeAsync();

    sealed class TestClock : TimeProvider
    {
        internal long Timestamp { get; set; }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Timestamp;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => Substitute.For<ITimer>();
    }
}
