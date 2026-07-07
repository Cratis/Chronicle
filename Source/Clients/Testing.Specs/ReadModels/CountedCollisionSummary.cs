// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that sources <see cref="Location"/> from <see cref="ArrangementSet"/> and only counts
/// <see cref="CandidateSubmitted"/> — which happens to carry an identically named <c>Location</c>. Unlike
/// <see cref="ArrangementSummary"/> it carries NO <c>[NoAutoMap]</c>: the aggregate heuristic must recognize
/// that <see cref="CandidateSubmitted"/> is subscribed only to be counted and therefore not auto-map its
/// <c>Location</c> over the explicitly sourced value.
/// </summary>
/// <param name="Id">Summary identifier.</param>
/// <param name="Location">The location, sourced only from <see cref="ArrangementSet"/> — no explicit opt-out.</param>
/// <param name="CandidateCount">The number of <see cref="CandidateSubmitted"/> events observed.</param>
[Passive]
[FromEvent<ArrangementSet>]
public record CountedCollisionSummary(
    [Key] Guid Id,

    [SetFrom<ArrangementSet>]
    string Location,

    [Count<CandidateSubmitted>]
    int CandidateCount);
