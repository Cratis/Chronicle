// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes the delivery that is currently being made to an observer.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> of the event store the delivery belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the events are delivered for.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the observer, or <see cref="ObserverId.Unspecified"/> when it has none.</param>
/// <param name="Kind">The <see cref="ObserverDeliveryKind"/>.</param>
/// <param name="ArtifactType">The <see cref="Type"/> of the artifact the delivery is made to.</param>
public sealed record ObserverDeliveryContext(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ObserverId ObserverId,
    ObserverDeliveryKind Kind,
    Type ArtifactType)
{
    static readonly AsyncLocal<ObserverDeliveryContext?> _current = new();

    /// <summary>
    /// Gets the context of the delivery in the current flow, or null when outside a delivery.
    /// </summary>
    public static ObserverDeliveryContext? Current
    {
        get => _current.Value;
        internal set => _current.Value = value;
    }

    /// <summary>
    /// Gets the delivered namespace for an event store, when the current flow is a delivery for that store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to look for.</param>
    /// <returns>The delivered namespace, or null.</returns>
    internal static EventStoreNamespaceName? NamespaceFor(EventStoreName eventStore) =>
        Current is { } current && current.EventStore == eventStore ? current.Namespace : null;
}
