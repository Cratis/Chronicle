// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement;

/// <summary>
/// Represents a placement strategy for observers that respects the Clustering configuration.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class ObserverPlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="ObserverPlacementStrategy"/>.
    /// </summary>
    internal static readonly ObserverPlacementStrategy Singleton = new();
}
