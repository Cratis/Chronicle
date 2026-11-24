// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Supervisors;

/// <summary>
/// Represents the key of an <see cref="IObserverSupervisor"/>: one per event store and observer, whatever the namespace.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the observer.</param>
public record ObserverSupervisorKey(EventStoreName EventStore, ObserverId ObserverId)
{
    /// <summary>
    /// Implicitly convert from <see cref="ObserverSupervisorKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverSupervisorKey"/> to convert from.</param>
    public static implicit operator string(ObserverSupervisorKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, ObserverId);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverSupervisorKey"/> instance.</returns>
    public static ObserverSupervisorKey Parse(string key) => KeyHelper.Parse<ObserverSupervisorKey>(key);
}
