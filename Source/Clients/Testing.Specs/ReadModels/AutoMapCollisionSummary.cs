// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that subscribes to two events sharing a <c>Location</c> property name — one only for a
/// <c>[Count]</c> — and declares an explicit source for <see cref="Location"/>. Used to verify that the
/// explicit source is authoritative and AutoMap does not bleed the other event's same-named value in.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Location">The work location, sourced explicitly from <see cref="WorkArrangementSet"/>.</param>
/// <param name="CandidateCount">The number of submitted candidates.</param>
[Passive]
[FromEvent<WorkArrangementSet>]
[FromEvent<CandidateSubmitted>]
public record AutoMapCollisionSummary(
    [Key] Guid Id,

    [SetFrom<WorkArrangementSet>(nameof(WorkArrangementSet.Location))]
    string Location,

    [Count<CandidateSubmitted>]
    int CandidateCount);
