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
/// Identifies and removes recommendations for removed observers and superseded requests.
/// </summary>
internal static class IgnoredRecommendations
{
    /// <summary>
    /// Gets recommendations for an observer removed from this namespace.
    /// </summary>
    /// <param name="all">The namespace's recommendations.</param>
    /// <param name="observerId">The removed observer.</param>
    /// <param name="eventSequenceId">The observer's event sequence.</param>
    /// <returns>The recommendations about the removed observer.</returns>
    internal static IEnumerable<RecommendationState> ForRemovedObserver(IEnumerable<RecommendationState> all, ObserverId observerId, EventSequenceId eventSequenceId) =>
        all.Where(row => row.Request switch
        {
            ReplayCandidateRequest request => request.ObserverId == observerId && request.ObserverKey.EventSequenceId == eventSequenceId,
            IgnoredProjectionRegistrationRequest request => request.ProjectionId.Value == observerId.Value,
            _ => false
        });

    /// <summary>
    /// Gets ignored registrations superseded by a newer stored version and replay recommendations superseded by a definition change.
    /// </summary>
    /// <param name="all">The namespace's recommendations.</param>
    /// <param name="type">The incoming recommendation type.</param>
    /// <param name="incoming">The incoming request.</param>
    /// <returns>The superseded recommendations.</returns>
    internal static IEnumerable<RecommendationState> SupersededBy(IEnumerable<RecommendationState> all, RecommendationType type, IRecommendationRequest incoming) =>
        all.Where(row => row.Type == type && IsSuperseded(row, incoming));

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

    static bool IsSuperseded(RecommendationState row, IRecommendationRequest incoming) => (row.Request, incoming) switch
    {
        (IgnoredProjectionRegistrationRequest ignored, IgnoredProjectionRegistrationRequest request) =>
            row.IsIgnored && ignored.ProjectionId == request.ProjectionId && ProjectionVersions.IsOlder(ignored.StoredVersion, request.StoredVersion),
        (ReplayCandidateRequest previous, ReplayCandidateRequest request) =>
            request.ChangeId.Length > 0 && previous.ChangeId != request.ChangeId &&
            previous.ObserverId == request.ObserverId && previous.ObserverKey == request.ObserverKey &&
            previous.Reasons.Select(reason => reason.Type).ToHashSet().SetEquals(request.Reasons.Select(reason => reason.Type)),
        _ => false
    };
}
