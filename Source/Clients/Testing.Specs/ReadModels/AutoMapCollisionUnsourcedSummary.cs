// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that subscribes to two events sharing a <c>Location</c> property name but declares <b>no</b>
/// explicit source for <see cref="Location"/>. Used to verify that when a colliding property is not
/// explicitly mapped, name-based AutoMap still applies from every subscribed event (last one wins) — the
/// explicit-beats-implicit suppression is scoped to explicitly-mapped properties only.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Location">The location, mapped purely by AutoMap from whichever event carries it.</param>
/// <param name="CandidateCount">The number of submitted candidates.</param>
[Passive]
[FromEvent<WorkArrangementSet>]
[FromEvent<CandidateSubmitted>]
public record AutoMapCollisionUnsourcedSummary(
    [Key] Guid Id,

    string Location,

    [Count<CandidateSubmitted>]
    int CandidateCount);
