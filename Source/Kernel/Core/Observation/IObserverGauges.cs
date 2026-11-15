// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the process-wide registry the observer gauges are sampled from.
/// </summary>
/// <remarks>
/// Each process reports only the observers it hosts. An observer activation registers once it owns a live observer,
/// publishes snapshots when its state is persisted or transitions, and releases its entry when it deactivates.
/// The registry never holds a reference to the activation itself.
/// </remarks>
public interface IObserverGauges
{
    /// <summary>
    /// Registers a resident observer.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="observerType">The <see cref="ObserverType"/> of the observer.</param>
    /// <returns>The entry to publish snapshots to; releasing it removes the observer from the gauges.</returns>
    ObserverGaugeEntry Register(ObserverKey observerKey, ObserverType observerType);
}
