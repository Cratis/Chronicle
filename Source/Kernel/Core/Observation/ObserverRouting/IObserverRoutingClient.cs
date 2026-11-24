// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Defines a client that tells the routing table of every active silo the revision of an observer's definition and
/// collects their acknowledgements.
/// </summary>
public interface IObserverRoutingClient
{
    /// <summary>
    /// Publish a revision of an observer's definition to every active silo, once storage holds it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> whose definition was revised.</param>
    /// <param name="revision">The stored <see cref="ObserverDefinitionRevision"/>; <see cref="ObserverDefinitionRevision.None"/> for a deleted definition.</param>
    /// <returns>The <see cref="ObserverRevisionAcknowledgements"/>.</returns>
    /// <remarks>
    /// The silo is told only the revision and reads the definition itself. A silo that does not answer, for instance one of
    /// an earlier release without the service, is reported as lagging rather than failing the publication.
    /// </remarks>
    Task<ObserverRevisionAcknowledgements> Publish(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision);
}
