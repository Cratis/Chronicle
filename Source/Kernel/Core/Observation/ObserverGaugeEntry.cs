// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a resident observer in the <see cref="IObserverGauges"/>.
/// </summary>
/// <remarks>
/// The entry holds only the observer's identity, its precomputed attributes and the last published snapshot, so a
/// gauge callback reading it never reaches into an observer activation.
/// </remarks>
public sealed class ObserverGaugeEntry
{
    readonly Action<ObserverGaugeEntry> _release;
    ObserverGaugeSnapshot _snapshot = ObserverGaugeSnapshot.Initial;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverGaugeEntry"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="observerType">The <see cref="ObserverType"/> of the observer.</param>
    /// <param name="release">Removes the entry from its registry.</param>
    internal ObserverGaugeEntry(ObserverKey observerKey, ObserverType observerType, Action<ObserverGaugeEntry> release)
    {
        ObserverKey = observerKey;
        ObserverType = observerType;
        _release = release;
        Tags =
        [
            new(WellKnownTelemetryAttributes.EventStoreName, observerKey.EventStore.Value),
            new(WellKnownTelemetryAttributes.EventStoreNamespace, observerKey.Namespace.Value),
            new(WellKnownTelemetryAttributes.EventSequenceId, observerKey.EventSequenceId.Value),
            new(WellKnownTelemetryAttributes.ObserverId, observerKey.ObserverId.Value)
        ];
    }

    /// <summary>
    /// Gets the <see cref="ObserverKey"/> of the observer.
    /// </summary>
    public ObserverKey ObserverKey { get; }

    /// <summary>
    /// Gets the <see cref="ObserverType"/> of the observer.
    /// </summary>
    public ObserverType ObserverType { get; }

    /// <summary>
    /// Gets the last published snapshot.
    /// </summary>
    public ObserverGaugeSnapshot Snapshot => Volatile.Read(ref _snapshot);

    /// <summary>
    /// Gets the per-observer attributes: event store, namespace, event sequence and observer id.
    /// </summary>
    internal KeyValuePair<string, object?>[] Tags { get; }

    /// <summary>
    /// Publishes a snapshot, replacing the previous one.
    /// </summary>
    /// <param name="snapshot">The <see cref="ObserverGaugeSnapshot"/> to publish.</param>
    public void Publish(ObserverGaugeSnapshot snapshot) => Volatile.Write(ref _snapshot, snapshot);

    /// <summary>
    /// Removes the observer from the gauges, unless a newer activation registered it since.
    /// </summary>
    public void Release() => _release(this);
}
