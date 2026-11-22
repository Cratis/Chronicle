// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>Compares the frozen multi-pass partition-run algorithm with the real preparation helper.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("ObserverRunPreparation")]
public class ObserverRunPreparationBenchmark
{
    static readonly EventType _subscribedType = new("subscribed", 1);
    static readonly EventType _otherType = new("other", 1);
    static readonly FrozenSet<EventTypeId> _subscribedIds = new[] { _subscribedType.Id }.ToFrozenSet();
    static readonly FrozenSet<EventTypeId> _missingIds = new[] { new EventTypeId("missing") }.ToFrozenSet();
    static readonly EventSourceType _wantedSource = new("wanted-source");
    static readonly EventStreamType _wantedStream = new("wanted-stream");
    AppendedEvent[] _events = null!;
    ObserverRunPreparationContext _context;

    /// <summary>Gets or sets the number of events in the actual array-backed partition run.</summary>
    [Params(1, 4096)]
    public int RunSize { get; set; }

    /// <summary>Gets or sets the applicability/filter control.</summary>
    [Params("all", "selective", "excluded", "late")]
    public string Scenario { get; set; } = "all";

    /// <summary>Prepares immutable inputs and validates the paired algorithms outside measurement.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _events = [.. Enumerable.Range(0, RunSize).Select(CreateEvent)];
        _context = ContextForScenario();
        VerifyEquivalent(PrepareBaseline(), PrepareCurrent(), Scenario != "excluded");
    }

    /// <summary>Executes the benchmark-only copy of the unchanged multi-pass algorithm.</summary>
    /// <returns>All result fields, exposing the internal result as an allocation-free public tuple.</returns>
    [Benchmark(Baseline = true)]
    public (EventSequenceNumber ObservedTail, bool IsApplicable, AppendedEvent[] Events) PrepareBaseline() =>
        ResultOf(PrepareBaselineRun(_events, _context));

    /// <summary>Executes the actual internal production helper, not a simulated candidate.</summary>
    /// <returns>All result fields with the same tuple representation as the baseline.</returns>
    [Benchmark]
    public (EventSequenceNumber ObservedTail, bool IsApplicable, AppendedEvent[] Events) PrepareCurrent() =>
        ResultOf(ObserverRunPreparation.Prepare(_events, _context));

    static (EventSequenceNumber ObservedTail, bool IsApplicable, AppendedEvent[] Events) ResultOf(PreparedObserverRun prepared) =>
        (prepared.ObservedTail, prepared.IsApplicable, prepared.EventsToHandle);

    static void VerifyEquivalent(
        (EventSequenceNumber ObservedTail, bool IsApplicable, AppendedEvent[] Events) baseline,
        (EventSequenceNumber ObservedTail, bool IsApplicable, AppendedEvent[] Events) current,
        bool shouldApply)
    {
        if (!(baseline.ObservedTail, baseline.IsApplicable).Equals((current.ObservedTail, current.IsApplicable)))
        {
            throw new InvalidOperationException("Partition-run baseline and production decisions disagree.");
        }

        if (!baseline.Events.SequenceEqual(current.Events))
        {
            throw new InvalidOperationException("Partition-run baseline and production events disagree.");
        }

        if (current.IsApplicable != shouldApply)
        {
            throw new InvalidOperationException("The partition-run scenario did not exercise its expected path.");
        }
    }

    /// <summary>Freezes the extracted multi-pass algorithm before any single-enumeration change.</summary>
    /// <param name="events">The original nonempty run.</param>
    /// <param name="context">The same prepared observer inputs used by production.</param>
    /// <returns>The observed tail, applicability decision and original cursor-retained events.</returns>
    static PreparedObserverRun PrepareBaselineRun(IEnumerable<AppendedEvent> events, ObserverRunPreparationContext context)
    {
        var observedTail = events.Last().Context.SequenceNumber;
        if (BaselineHasNoApplicableEvents(events, context))
        {
            return new(observedTail, false, []);
        }

        var nextEventSequenceNumber = context.NextEventSequenceNumber;
        var eventsToHandle = events.Where(appendedEvent => appendedEvent.Context.SequenceNumber >= nextEventSequenceNumber).ToArray();
        return new(observedTail, true, eventsToHandle);
    }

    static bool BaselineHasNoApplicableEvents(IEnumerable<AppendedEvent> events, ObserverRunPreparationContext context) =>
        (!context.SubscribesToAllEvents && !BaselineHasSubscribedEvent(events, (FrozenSet<EventTypeId>)context.EventTypeIds)) || BaselineExcludedByFilters([.. events], context.Filters);

    static bool BaselineHasSubscribedEvent(IEnumerable<AppendedEvent> events, FrozenSet<EventTypeId> eventTypeIds)
    {
        foreach (var appendedEvent in events)
        {
            if (eventTypeIds.Contains(appendedEvent.Context.EventType.Id)) return true;
        }

        return false;
    }

    static bool BaselineExcludedByFilters(IReadOnlyCollection<AppendedEvent> events, ObserverFilters? filters)
    {
        if (filters is null)
        {
            return false;
        }

        return !BaselineMatchesSourceType(events, filters) || !BaselineMatchesStreamType(events, filters) || !BaselineMatchesTags(events, filters);
    }

    static bool BaselineMatchesSourceType(IReadOnlyCollection<AppendedEvent> events, ObserverFilters filters) =>
        filters.EventSourceType is not { } eventSourceType || eventSourceType == EventSourceType.Unspecified ||
        events.Any(appendedEvent => appendedEvent.Context.EventSourceType == eventSourceType);

    static bool BaselineMatchesStreamType(IReadOnlyCollection<AppendedEvent> events, ObserverFilters filters) =>
        filters.EventStreamType is not { } eventStreamType || eventStreamType.IsAll ||
        events.Any(appendedEvent => appendedEvent.Context.EventStreamType == eventStreamType);

    static bool BaselineMatchesTags(IReadOnlyCollection<AppendedEvent> events, ObserverFilters filters) =>
        !filters.Tags.Any() || events.Any(appendedEvent => appendedEvent.Context.Tags.Any(tag => filters.Tags.Contains(tag.Value)));

    ObserverRunPreparationContext ContextForScenario()
    {
        EventSequenceNumber floor = (ulong)(RunSize / 2);
        var filters = new ObserverFilters(["wanted"], _wantedSource, _wantedStream);
        return Scenario switch
        {
            "all" => new(floor, true, _subscribedIds, null),
            "selective" or "late" => new(floor, false, _subscribedIds, filters),
            "excluded" => new(floor, false, _missingIds, filters),
            _ => throw new InvalidOperationException("Unknown partition-run benchmark scenario.")
        };
    }

    EventType EventTypeFor(int index) => Scenario switch
    {
        "late" => index == RunSize - 1 ? _subscribedType : _otherType,
        _ => index % 2 == 0 ? _subscribedType : _otherType
    };

    AppendedEvent CreateEvent(int index)
    {
        var eventType = EventTypeFor(index);
        var appendedEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, (ulong)index);
        if (RunSize == 1)
        {
            return appendedEvent with
            {
                Context = appendedEvent.Context with { EventSourceType = _wantedSource, EventStreamType = _wantedStream, Tags = [new("wanted")] }
            };
        }

        return appendedEvent with
        {
            Context = appendedEvent.Context with
            {
                EventSourceType = index % 3 == 0 ? _wantedSource : new("other-source"),
                EventStreamType = index % 3 == 1 ? _wantedStream : new("other-stream"),
                Tags = index % 3 == 2 ? [new("wanted")] : []
            }
        };
    }
}
