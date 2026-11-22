// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlProviderHost;

/// <summary>Verifies event round trips, tails and table isolation in the benchmark SQL adapter.</summary>
public class when_draining_isolated_sequences : Specification
{
    readonly List<AppendedEvent> _events = [];
    SqlProviderHost _host = null!;
    ulong _tail;
    ulong _otherTail;
    ulong _selectedTail;
    ulong _unselectedTail;
    bool _appended;

    async Task Establish()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
    }

    async Task Because()
    {
        var storage = _host.CreateSequence();
        var other = _host.CreateSequence();
        _appended = (await storage.AppendMany(Enumerable.Range(0, 20).Select(i => BenchmarkEvents.ToAppend((ulong)i, 256)))).IsSuccess;
        using var cursor = await storage.GetFromSequenceNumber(0);
        while (await cursor.MoveNext()) _events.AddRange(cursor.Current);
        _tail = await storage.GetTailSequenceNumber();
        _otherTail = await other.GetTailSequenceNumber();
        var tails = await storage.GetTailSequenceNumbersForEventTypes([BenchmarkEvents.Selected, BenchmarkEvents.Other]);
        _selectedTail = tails[BenchmarkEvents.Selected];
        _unselectedTail = tails[BenchmarkEvents.Other];
    }

    async Task Destroy() => await _host.DisposeAsync();

    [Fact] void should_append_successfully() => _appended.ShouldBeTrue();
    [Fact] void should_drain_every_event() => _events.Count.ShouldEqual(20);
    [Fact] void should_preserve_order() => _events.Select(e => e.Context.SequenceNumber.Value).ShouldEqual(Enumerable.Range(0, 20).Select(i => (ulong)i));
    [Fact] void should_preserve_both_types() => _events.Select(e => e.Context.EventType).Distinct().Count().ShouldEqual(2);
    [Fact] void should_preserve_payloads() => _events.TrueForAll(e => ((IDictionary<string, object?>)e.Content)["value"] is string { Length: 256 }).ShouldBeTrue();
    [Fact] void should_resolve_system_identity() => _events.TrueForAll(e => e.Context.CausedBy == Identity.System).ShouldBeTrue();
    [Fact] void should_read_the_tail() => _tail.ShouldEqual(19UL);
    [Fact] void should_keep_the_other_table_empty() => _otherTail.ShouldEqual(EventSequenceNumber.Unavailable.Value);
    [Fact] void should_read_selected_tail() => _selectedTail.ShouldEqual(18UL);
    [Fact] void should_read_other_tail() => _unselectedTail.ShouldEqual(19UL);
}
