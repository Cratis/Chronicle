// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Defines the silo-local, in-memory read side of observer definitions: the routing table of every event store.
/// </summary>
/// <remarks>
/// It is a read-through of shared storage, not an independent cache. A change is replicated by revision: the writer stores
/// the definition with a new revision, tells every silo the revision, and each silo reads the definition again only when the
/// revision it holds is lower.
/// </remarks>
public interface IObservers
{
    /// <summary>
    /// Gets the current routing table of an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <returns>The current <see cref="ObserverRoutingTable"/>.</returns>
    ValueTask<ObserverRoutingTable> GetTable(EventStoreName eventStore);

    /// <summary>
    /// Makes the table of the silo hold at least a revision of an observer's definition, reading it again when it is behind.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> whose definition was revised.</param>
    /// <param name="revision">The <see cref="ObserverDefinitionRevision"/> the writer stored.</param>
    /// <returns>The revision the silo holds afterwards; <see cref="ObserverDefinitionRevision.None"/> when it holds no definition.</returns>
    /// <remarks>
    /// The answer is the acknowledgement: a silo has applied the revision when the answer is at least that revision. A
    /// deleted definition is acknowledged by holding none.
    /// </remarks>
    Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision);
}
