// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Indexes one manager activation's recommendations, verifying persisted rows on every hit.
/// </summary>
/// <param name="storage">The namespace's recommendation storage.</param>
internal sealed class RecommendationIndex(IRecommendationStorage storage)
{
    readonly Dictionary<(RecommendationType Type, string RequestJson), RecommendationId> _requests = [];
    readonly Dictionary<string, HashSet<RecommendationId>> _subjects = [];
    readonly Dictionary<RecommendationId, (RecommendationType Type, string RequestJson, string Subject)> _entries = [];
    bool _loaded;

    /// <summary>
    /// Finds a matching persisted recommendation, loading the index once per activation.
    /// </summary>
    /// <param name="type">The recommendation type.</param>
    /// <param name="request">The request to match.</param>
    /// <returns>The matching identifier, or null.</returns>
    internal async Task<RecommendationId?> Find(RecommendationType type, IRecommendationRequest request)
    {
        if (!_loaded)
        {
            foreach (var row in await storage.GetAll())
            {
                Add(row.Id, row.Type, row.Request);
            }
            _loaded = true;
        }

        return _requests.TryGetValue((type, Serialize(request)), out var id) && await Get(id) is not null ? id : null;
    }

    /// <summary>
    /// Reads only the persisted rows for the request's subject.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>The subject's remaining rows.</returns>
    internal async Task<IEnumerable<RecommendationState>> CandidatesFor(IRecommendationRequest request)
    {
        var rows = new List<RecommendationState>();
        if (_subjects.TryGetValue(Subject(request), out var ids))
        {
            foreach (var id in ids.ToArray())
            {
                if (await Get(id) is { } row)
                {
                    rows.Add(row);
                }
            }
        }
        return rows;
    }

    /// <summary>
    /// Indexes a persisted recommendation.
    /// </summary>
    /// <param name="id">The recommendation identifier.</param>
    /// <param name="type">The recommendation type.</param>
    /// <param name="request">The persisted request.</param>
    internal void Add(RecommendationId id, RecommendationType type, IRecommendationRequest request)
    {
        var json = Serialize(request);
        var subject = Subject(request);
        _requests[(type, json)] = id;
        _entries[id] = (type, json, subject);
        if (subject.Length == 0)
        {
            return;
        }
        if (!_subjects.TryGetValue(subject, out var ids))
        {
            _subjects[subject] = ids = [];
        }
        ids.Add(id);
    }

    /// <summary>
    /// Forgets a recommendation removed from storage.
    /// </summary>
    /// <param name="id">The removed identifier.</param>
    internal void Remove(RecommendationId id)
    {
        if (_entries.Remove(id, out var entry))
        {
            var requestKey = (entry.Type, entry.RequestJson);
            if (_requests.TryGetValue(requestKey, out var mappedId) && mappedId == id)
            {
                _requests.Remove(requestKey);
            }
            if (_subjects.TryGetValue(entry.Subject, out var ids))
            {
                ids.Remove(id);
            }
        }
    }

    static string Serialize(IRecommendationRequest request) => JsonSerializer.Serialize((object)request);

    static string Subject(IRecommendationRequest request) => request switch
    {
        ReplayCandidateRequest replay => replay.ObserverId.Value,
        IgnoredProjectionRegistrationRequest registration => registration.ProjectionId.Value,
        _ => string.Empty
    };

    async Task<RecommendationState?> Get(RecommendationId id)
    {
        var row = await storage.Get(id);
        if (row is null)
        {
            Remove(id);
        }
        return row;
    }
}
