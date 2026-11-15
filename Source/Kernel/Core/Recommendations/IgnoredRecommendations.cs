// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Identifies and removes ignored recommendations that can no longer match a future request.
/// </summary>
internal static class IgnoredRecommendations
{
    /// <summary>
    /// Gets ignored recommendations for an observer removed from this namespace.
    /// </summary>
    /// <param name="all">The namespace's recommendations.</param>
    /// <param name="observerId">The removed observer.</param>
    /// <param name="eventSequenceId">The observer's event sequence.</param>
    /// <returns>The ignored recommendations about the removed observer.</returns>
    internal static IEnumerable<RecommendationState> ForRemovedObserver(IEnumerable<RecommendationState> all, ObserverId observerId, EventSequenceId eventSequenceId) =>
        all.Where(row => row.IsIgnored && row.Request switch
        {
            ReplayCandidateRequest request => request.ObserverId == observerId && request.ObserverKey.EventSequenceId == eventSequenceId,
            IgnoredProjectionRegistrationRequest request => request.ProjectionId.Value == observerId.Value,
            _ => false
        });

    /// <summary>
    /// Gets ignored projection registration recommendations superseded by a newer stored version.
    /// </summary>
    /// <param name="all">The namespace's recommendations.</param>
    /// <param name="type">The incoming recommendation type.</param>
    /// <param name="incoming">The incoming request.</param>
    /// <returns>The ignored recommendations whose stored version can no longer recur.</returns>
    internal static IEnumerable<RecommendationState> SupersededBy(IEnumerable<RecommendationState> all, RecommendationType type, IRecommendationRequest incoming) =>
        all.Where(row => row.IsIgnored && row.Type == type &&
            incoming is IgnoredProjectionRegistrationRequest request &&
            row.Request is IgnoredProjectionRegistrationRequest ignored &&
            ignored.ProjectionId == request.ProjectionId &&
            ProjectionVersions.IsOlder(ignored.StoredVersion, request.StoredVersion));

    /// <summary>
    /// Removes the selected recommendations, propagating any storage failure.
    /// </summary>
    /// <param name="storage">The namespace's recommendation storage.</param>
    /// <param name="rows">The recommendations to remove.</param>
    /// <returns>The number of recommendations removed.</returns>
    internal static async Task<int> Remove(IRecommendationStorage storage, IEnumerable<RecommendationState> rows)
    {
        var count = 0;
        foreach (var row in rows)
        {
            await storage.Remove(row.Id);
            count++;
        }
        return count;
    }
}
