// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Services;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Defines a service that lives in each silo and makes that silo's routing table hold a revision of an observer's definition.
/// </summary>
public interface IObserverRoutingGrainService : IGrainService
{
    /// <summary>
    /// Make the routing table of the local silo hold at least a revision of an observer's definition.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> whose definition was revised.</param>
    /// <param name="revision">The <see cref="ObserverDefinitionRevision"/> that was stored.</param>
    /// <returns>The revision the silo holds afterwards.</returns>
    Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision);
}
