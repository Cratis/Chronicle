// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Decides whether a partition replay rebuilds the partition in isolation and publishes it atomically.
/// </summary>
/// <remarks>
/// Every condition must hold; anything else replays the partition in place exactly as before. A subset of event types
/// cannot be rebuilt from empty without dropping what the other event types contributed, and a store or projection
/// touched by materialization generations serves from a binding that an isolated publish does not resolve.
/// </remarks>
public static class PartitionRebuildEligibility
{
    /// <summary>
    /// Check whether a partition replay is eligible for an isolated rebuild.
    /// </summary>
    /// <param name="features">The configured <see cref="Features"/>.</param>
    /// <param name="candidate">The <see cref="PartitionRebuildCandidate"/> describing the replay.</param>
    /// <returns>True if the partition is rebuilt in isolation; false if it is replayed in place.</returns>
    public static bool IsEligible(Features features, PartitionRebuildCandidate candidate) =>
        features.IsolatedPartitionReplay &&
        !features.ProjectionGenerations &&
        candidate.ObserverType == ObserverType.Projection &&
        candidate.ReplaysAllEventTypes &&
        candidate.SinkSupportsIsolatedReplace &&
        candidate.UntouchedByGenerations &&
        PartitionReplayabilityClassifier.Classify(candidate.ObserverType, candidate.Definition) == PartitionReplayability.Closed;
}
