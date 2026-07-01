// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Location"/> is explicitly sourced from <b>two</b> different events. Used to
/// verify that multiple explicit sources for one property still aggregate normally (last event applied
/// wins) — the explicit-beats-implicit suppression only removes name-based AutoMap, never explicit mappings.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Location">The location, explicitly sourced from both events.</param>
[Passive]
[FromEvent<WorkArrangementSet>]
[FromEvent<CandidateSubmitted>]
public record MultiSourceLocationSummary(
    [Key] Guid Id,

    [SetFrom<WorkArrangementSet>(nameof(WorkArrangementSet.Location))]
    [SetFrom<CandidateSubmitted>(nameof(CandidateSubmitted.Location))]
    string Location);
