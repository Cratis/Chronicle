// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.Placement;

/// <summary>
/// Represents a placement strategy for connected observers to guarantee they run on the same silo as a connected client.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class ConnectedObserverPlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="ConnectedObserverPlacementStrategy"/>.
    /// </summary>
    internal static readonly ConnectedObserverPlacementStrategy Singleton = new();
}
