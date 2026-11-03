// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what is known about a partition replay when deciding whether it can be rebuilt in isolation.
/// </summary>
/// <param name="ObserverType">The <see cref="ObserverType"/> of the observer being replayed.</param>
/// <param name="Definition">The <see cref="ProjectionDefinition"/> of the projection, or null when the observer is not a projection or it could not be read.</param>
/// <param name="ReplaysAllEventTypes">Whether the replay covers every event type the observer handles.</param>
/// <param name="SinkSupportsIsolatedReplace">Whether the projection's sink can atomically replace one instance.</param>
/// <param name="UntouchedByGenerations">Whether the store has no generation initialization receipt and the projection is not bound to a generation.</param>
public record PartitionRebuildCandidate(
    ObserverType ObserverType,
    ProjectionDefinition? Definition,
    bool ReplaysAllEventTypes,
    bool SinkSupportsIsolatedReplace,
    bool UntouchedByGenerations);
