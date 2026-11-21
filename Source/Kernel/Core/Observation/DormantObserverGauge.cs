// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IDormantObserverGauge"/>.
/// </summary>
/// <remarks>
/// <para>
/// Reports how many observers the appended events queues of this silo hold as dormant, per event store and namespace. A
/// dormant observer has no activation, so it is missing from <see cref="WellKnownMetricNames.ObserverResident"/>; this is where it
/// is counted instead. A queue registers while it is active and gives its place back when it is deactivated, so nothing is
/// left behind, and a combination that once had dormant observers keeps reporting zero once they are gone.
/// </para>
/// </remarks>
[Singleton]
public sealed class DormantObserverGauge : IDormantObserverGauge
{
    readonly ConcurrentDictionary<object, Registration> _queues = new();
    readonly ConcurrentDictionary<Group, byte> _groups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DormantObserverGauge"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> to report to.</param>
    public DormantObserverGauge([FromKeyedServices(WellKnown.MeterName)] IMeter<DormantObserverGauge> meter)
    {
        meter.ActualMeter?.CreateObservableGauge(
            WellKnownMetricNames.ObserverDormant,
            Observe,
            WellKnownMetricNames.ObserverDormantUnit,
            "Number of observers held as dormant on this silo: subscribed, with no activation");
    }

    /// <inheritdoc/>
    public IDisposable Register(EventSequenceKey eventSequenceKey, Func<int> count)
    {
        var token = new object();
        _queues[token] = new Registration(eventSequenceKey, count);
        _groups.TryAdd(new Group(eventSequenceKey.EventStore, eventSequenceKey.Namespace), 0);
        return new Handle(() => _queues.TryRemove(token, out _));
    }

    /// <summary>
    /// Gets the current measurements, one per event store and namespace that has held dormant observers.
    /// </summary>
    /// <returns>The measurements.</returns>
    internal IEnumerable<Measurement<int>> Observe()
    {
        var counts = _queues.Values
            .GroupBy(registration => new Group(registration.Key.EventStore, registration.Key.Namespace))
            .ToDictionary(group => group.Key, group => group.Sum(registration => registration.Count()));

        return _groups.Keys
            .Select(group => new Measurement<int>(
                counts.GetValueOrDefault(group),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, group.EventStore.Value),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, group.Namespace.Value)))
            .ToArray();
    }

    sealed record Registration(EventSequenceKey Key, Func<int> Count);

    sealed record Group(EventStoreName EventStore, EventStoreNamespaceName Namespace);

    sealed class Handle(Action release) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                release();
            }
        }
    }
}
