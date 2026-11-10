// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;

/// <summary>
/// A manager whose namespace spans more event stores than an erasure can reach one at a time within a grain call,
/// where every step against a store takes a moment to answer and is recorded as it starts and as it ends.
/// </summary>
public class many_event_stores : a_pii_manager
{
    protected const int NumberOfEventStores = 40;
    protected const string Fence = "fence";
    protected const string Sweep = "sweep";
    protected const string Evict = "evict";
    protected const string Confirm = "confirm";
    protected const string Compact = "compact";

    protected static readonly EventStoreName[] EventStores =
        [EventStore, .. Enumerable.Range(1, NumberOfEventStores - 1).Select(_ => new EventStoreName($"event-store-{_}"))];

    protected ConcurrentQueue<(string Phase, EventStoreName EventStore, bool Started)> _steps = new();
    protected int _maximumInFlight;
    int _inFlight;

    void Establish()
    {
        _storage.GetEventStores().Returns(EventStores);
        _keyStore
            .RecordErasureFor(Arg.Any<EventStoreName>(), EventStoreNamespace, Identifier, Arg.Any<EncryptionKeyErasureRequest>())
            .Returns(call => Step(Fence, call.ArgAt<EventStoreName>(0)));
        _keyStore
            .DeleteThroughFor(Arg.Any<EventStoreName>(), EventStoreNamespace, Identifier, Arg.Any<string>())
            .Returns(call => Step(Sweep, call.ArgAt<EventStoreName>(0)));
        _cacheClient
            .Evict(Arg.Any<EventStoreName>(), EventStoreNamespace, Identifier)
            .Returns(call => Step(Evict, call.ArgAt<EventStoreName>(0)));
        _keyStore
            .ConfirmErasureFor(Arg.Any<EventStoreName>(), EventStoreNamespace, Identifier, Arg.Any<string>())
            .Returns(call => Step(Confirm, call.ArgAt<EventStoreName>(0)));
        _keyStore
            .CompactErasureRequestsFor(Arg.Any<EventStoreName>(), EventStoreNamespace, Identifier, Arg.Any<int>())
            .Returns(call => Step(Compact, call.ArgAt<EventStoreName>(0)));
    }

    /// <summary>
    /// Every store of a phase must have answered before any store is asked for the next phase.
    /// </summary>
    /// <param name="earlier">The phase that has to complete first.</param>
    /// <param name="later">The phase that may only start afterwards.</param>
    protected void ShouldHaveCompletedBefore(string earlier, string later)
    {
        var steps = _steps.ToArray();
        var lastEnd = Array.FindLastIndex(steps, _ => _.Phase == earlier && !_.Started);
        var firstStart = Array.FindIndex(steps, _ => _.Phase == later && _.Started);
        lastEnd.ShouldBeGreaterThanOrEqual(0);
        firstStart.ShouldBeGreaterThan(lastEnd);
    }

    /// <summary>
    /// Gets the event stores a phase reached.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <returns>The event stores, in the order they were reached.</returns>
    protected IEnumerable<EventStoreName> Reached(string phase) => _steps.Where(_ => _.Phase == phase && _.Started).Select(_ => _.EventStore);

    async Task Step(string phase, EventStoreName eventStore)
    {
        _steps.Enqueue((phase, eventStore, true));
        var inFlight = Interlocked.Increment(ref _inFlight);
        int maximum;
        do
        {
            maximum = Volatile.Read(ref _maximumInFlight);
        }
        while (inFlight > maximum && Interlocked.CompareExchange(ref _maximumInFlight, inFlight, maximum) != maximum);

        await Task.Delay(TimeSpan.FromMilliseconds(20), TimeProvider.System);

        Interlocked.Decrement(ref _inFlight);
        _steps.Enqueue((phase, eventStore, false));
    }
}
