// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Generations;

/// <summary>
/// Reads compatibility metadata without opening migrating event-store accessors.
/// </summary>
public interface IGenerationFloorDiscovery
{
    /// <summary>
    /// Reads floor-bearing stores independently of the resettable registry, without provisioning storage.
    /// </summary>
    /// <returns>The durable floors by store.</returns>
    Task<IReadOnlyDictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>> ReadGenerationFloors();
}
