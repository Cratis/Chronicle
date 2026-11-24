// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Defines the state provider the routing table reads observer definitions through, built on the Storage APIs.
/// </summary>
public interface IObserverDefinitionsStateProvider
{
    /// <summary>
    /// Gets the current snapshot of the definitions of an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <returns>The current <see cref="ObserverDefinitionRegistrySnapshot"/>.</returns>
    ValueTask<ObserverDefinitionRegistrySnapshot> GetSnapshot(EventStoreName eventStore);

    /// <summary>
    /// Gets the revision of an observer's definition as storage holds it, past anything held in memory.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <returns>The stored <see cref="ObserverDefinitionRevision"/>, or <see cref="ObserverDefinitionRevision.None"/> when nothing is stored.</returns>
    Task<ObserverDefinitionRevision> GetStoredRevision(EventStoreName eventStore, ObserverId observerId);

    /// <summary>
    /// Tells the provider that the definition of an observer changed, so the next snapshot reads it again.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> that changed.</param>
    void Changed(EventStoreName eventStore, ObserverId observerId);
}
