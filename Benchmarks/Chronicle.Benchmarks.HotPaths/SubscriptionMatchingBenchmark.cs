// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Measures production subscription matching without queue scheduling or reflection in the timed path.
/// </summary>
[MemoryDiagnoser]
public class SubscriptionMatchingBenchmark
{
    Func<AppendedEventsQueueObserverSubscription, HashSet<EventTypeId>, bool, bool> _mayMatch = null!;
    Func<IReadOnlyList<AppendedEvent>, AppendedEventsQueueObserverSubscription, IReadOnlyList<AppendedEvent>> _filter = null!;
    AppendedEventsQueueObserverSubscription _subscription = null!;
    AppendedEvent[] _events = null!;
    HashSet<EventTypeId> _batchTypes = null!;

    /// <summary>
    /// Gets or sets the number of subscribed IDs.
    /// </summary>
    public int SubscribedTypes { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 128;

    /// <summary>
    /// Gets or sets whether events match, miss, or bypass membership for an all-types subscription.
    /// </summary>
    public string Scenario { get; set; } = "Match";

    /// <summary>
    /// Creates subscription caches once, as production does when a subscription is installed.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        _mayMatch = typeof(AppendedEventsQueue).GetMethod("MayMatch", flags)!.CreateDelegate<Func<AppendedEventsQueueObserverSubscription, HashSet<EventTypeId>, bool, bool>>();
        _filter = typeof(AppendedEventsQueue).GetMethod("GetFilteredEvents", flags)!.CreateDelegate<Func<IReadOnlyList<AppendedEvent>, AppendedEventsQueueObserverSubscription, IReadOnlyList<AppendedEvent>>>();
        var ids = Enumerable.Range(0, SubscribedTypes).Select(index => new EventTypeId($"event-{index}")).ToArray();
        _subscription = new(new ObserverKey("observer", "store", "namespace", "sequence"), ids, AllEventTypes: Scenario == "All");
        var eventType = new EventType(Scenario == "Match" ? ids[^1] : new EventTypeId("unsubscribed"), EventTypeGeneration.First);
        _events = Enumerable.Range(0, BatchSize).Select(_ => AppendedEvent.EmptyWithEventType(eventType)).ToArray();
        _batchTypes = [eventType.Id];
    }

    /// <summary>
    /// Applies the production coarse batch prefilter.
    /// </summary>
    /// <returns>Whether this subscription might match.</returns>
    [Benchmark]
    public bool MayMatch() => _mayMatch(_subscription, _batchTypes, false);

    /// <summary>
    /// Applies the production per-event membership and filter checks.
    /// </summary>
    /// <returns>The matching events.</returns>
    [Benchmark]
    public IReadOnlyList<AppendedEvent> FilterEvents() => _filter(_events, _subscription);
}
