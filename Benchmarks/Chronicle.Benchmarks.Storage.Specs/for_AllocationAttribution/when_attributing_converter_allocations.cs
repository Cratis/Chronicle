// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Specifications;
using MongoDB.Bson;
using Xunit;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Fixture and real Mongo EventConverter boundaries, requiring no server or timing benchmark.</summary>
/// <param name="output">The specification output, including the allocation table.</param>
public class when_attributing_converter_allocations(ITestOutputHelper output) : Specification
{
    AllocationComparison[] _samples = [];

    void Because() => _samples = AllocationProbe.OnThread(Sample);

    [Fact]
    void should_report_lookup_converter_and_residual_accounting_boundaries()
    {
        foreach (var sample in _samples) sample.Write(output);
        _samples.Length.ShouldEqual(5);
        _samples[0].Clean.ShouldEqual(0d);
        _samples[1].Clean.ShouldEqual(0d);
        _samples[2].Clean.ShouldEqual(0d);
        (_samples[3].Legacy > _samples[3].Clean).ShouldBeTrue();
        (_samples[3].Clean > 0).ShouldBeTrue();
        _samples[4].Clean.ShouldEqual(_samples[4].Legacy);
    }

    static AllocationComparison[] Sample()
    {
        AllocationProbe.Bytes(() => { }).ShouldEqual(0d);
        var oldTypes = LegacyFixtures.EventTypes();
        var newTypes = BenchmarkEvents.CreateEventTypes();
        var oldIdentities = LegacyFixtures.Identities();
        var newIdentities = BenchmarkEvents.CreateIdentities();
        IdentityId[] chain = [];
        var events = Enumerable.Range(0, SqlAllocationFixture.EventCount).Select(i => StoredEvent((ulong)i)).ToArray();
        var legacy = Converter(oldTypes, oldIdentities);
        var clean = Converter(newTypes, newIdentities);
        foreach (var type in new[] { BenchmarkEvents.Selected, BenchmarkEvents.Other })
        {
            AllocationProbe.Complete(oldTypes.HasFor(type.Id, type.Generation)).ShouldBeTrue();
            AllocationProbe.Complete(newTypes.HasFor(type.Id, type.Generation)).ShouldBeTrue();
        }
        Validate(legacy, events);
        Validate(clean, events);
        return
        [
            AllocationComparison.Measure("Fixture identity lookup", () => GC.KeepAlive(AllocationProbe.Complete(oldIdentities.GetFor(chain))), () => GC.KeepAlive(AllocationProbe.Complete(newIdentities.GetFor(chain)))),
            AllocationComparison.Measure("Fixture schema HasFor", () => HasFor(oldTypes), () => HasFor(newTypes)),
            AllocationComparison.Measure("Fixture schema GetFor", () => GC.KeepAlive(AllocationProbe.Complete(oldTypes.GetFor(BenchmarkEvents.Selected.Id))), () => GC.KeepAlive(AllocationProbe.Complete(newTypes.GetFor(BenchmarkEvents.Selected.Id)))),
            AllocationComparison.Measure("Mongo EventConverter inclusive", () => ConvertAll(legacy, events), () => ConvertAll(clean, events), events.Length, 16),
            Accounting()
        ];
    }

    static EventConverter Converter(IEventTypesStorage types, IIdentityStorage identities) =>
        new("test-store", "test-namespace", types, identities, new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));

    static Event StoredEvent(ulong sequenceNumber) => new(
        sequenceNumber,
        CorrelationId.NotSet,
        [],
        [],
        BenchmarkEvents.TypeAt(sequenceNumber).Id,
        DateTimeOffset.UnixEpoch,
        EventSourceType.Default,
        BenchmarkEvents.SourceAt(sequenceNumber),
        EventStreamType.All,
        EventStreamId.Default,
        [],
        new Dictionary<string, BsonDocument> { ["1"] = new("value", new string('x', 256)) },
        new Dictionary<string, string> { ["1"] = "hash" },
        []);

    static void HasFor(IEventTypesStorage types)
    {
        if (!AllocationProbe.Complete(types.HasFor(BenchmarkEvents.Selected.Id))) throw new InvalidOperationException("Schema-guided conversion must remain enabled.");
    }

    static void ConvertAll(IEventConverter converter, Event[] events)
    {
        foreach (var @event in events) GC.KeepAlive(AllocationProbe.Complete(converter.ToAppendedEvent(@event)));
    }

    static void Validate(IEventConverter converter, Event[] events)
    {
        foreach (var @event in events)
        {
            var result = AllocationProbe.Complete(converter.ToAppendedEvent(@event));
            result.Context.SequenceNumber.ShouldEqual(@event.SequenceNumber);
            result.Context.EventType.Id.ShouldEqual(@event.Type);
            result.Context.CausedBy.ShouldEqual(Identity.System);
            ((string)((IDictionary<string, object?>)result.Content)["value"]!).Length.ShouldEqual(256);
        }
    }

    static AllocationComparison Accounting()
    {
        var counter = new CommandCounter();
        counter.Record("select");
        void Record()
        {
            var before = counter.Snapshot();
            GC.KeepAlive(new InvocationRecord(1, 1, counter.Snapshot().Since(before), [TimeSpan.Zero], 0));
        }
        return AllocationComparison.Measure("Residual snapshot/InvocationRecord construction only", Record, Record);
    }
}
