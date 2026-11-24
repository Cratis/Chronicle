// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// An immutable view of every <see cref="ObserverDefinition"/> in one event store at one <see cref="Revision"/>.
/// </summary>
/// <remarks>
/// Readers hold on to a snapshot for as long as they need a consistent view. A change produces a new snapshot with a
/// higher revision and never changes one that has been handed out; definitions that did not change are shared by
/// reference between consecutive snapshots, and the same instance is what every namespace's observer holds.
/// </remarks>
/// <param name="Revision">The revision of the snapshot. Starts at 1 and increases by one every time the content changes.</param>
/// <param name="Definitions">The <see cref="ObserverDefinition"/> of every observer, keyed by identifier.</param>
public sealed record ObserverDefinitionRegistrySnapshot(long Revision, FrozenDictionary<ObserverId, ObserverDefinition> Definitions)
{
    /// <summary>
    /// Gets the empty snapshot, which is where an event store starts before anything has been loaded for it.
    /// </summary>
    public static ObserverDefinitionRegistrySnapshot Empty { get; } = new(0, FrozenDictionary<ObserverId, ObserverDefinition>.Empty);

    /// <summary>
    /// Try to get the definition of an observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <param name="definition">The <see cref="ObserverDefinition"/> when found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public bool TryGetDefinition(ObserverId observerId, out ObserverDefinition definition) => Definitions.TryGetValue(observerId, out definition!);
}
