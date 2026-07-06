// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Location"/> is set exclusively from <see cref="ArrangementSet"/> and flagged
/// <c>[NoAutoMap]</c>, while it also counts unrelated <see cref="CandidateSubmitted"/> events. Because
/// <see cref="CandidateSubmitted"/> carries an identically named <c>Location</c>, this verifies that a
/// property-level <c>[NoAutoMap]</c> stops AutoMap from letting the counted event overwrite the location.
/// </summary>
/// <param name="Id">Summary identifier.</param>
/// <param name="Location">The location, sourced only from <see cref="ArrangementSet"/>.</param>
/// <param name="CandidateCount">The number of <see cref="CandidateSubmitted"/> events observed.</param>
[Passive]
[FromEvent<ArrangementSet>]
public record ArrangementSummary(
    [Key] Guid Id,

    [SetFrom<ArrangementSet>]
    [NoAutoMap]
    string Location,

    [Count<CandidateSubmitted>]
    int CandidateCount);
