// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.GraphProjections.MultipleGraphs.Scores;

/// <summary>
/// The scores of the same people, owned by another graph projection that writes the same node label.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Score">The points.</param>
[Graph("OrgScores")]
[Node]
[FromEvent<OrgPointsAwarded>]
[FromEvent<OrgPointsRevoked>]
public record Person([Key] Guid PersonId, [AddFrom<OrgPointsAwarded>(nameof(OrgPointsAwarded.Points))] [SubtractFrom<OrgPointsRevoked>(nameof(OrgPointsRevoked.Points))] int Score);
