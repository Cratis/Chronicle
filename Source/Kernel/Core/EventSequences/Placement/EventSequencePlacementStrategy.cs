// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Placement;

/// <summary>
/// Represents a placement strategy for event sequences that respects the Clustering configuration.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class EventSequencePlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="EventSequencePlacementStrategy"/>.
    /// </summary>
    internal static readonly EventSequencePlacementStrategy Singleton = new();
}
