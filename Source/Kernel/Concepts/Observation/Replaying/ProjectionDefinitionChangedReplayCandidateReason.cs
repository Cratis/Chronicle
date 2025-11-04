// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when projection definition has changed.
/// </summary>
public record ProjectionDefinitionChangedReplayCandidateReason()
    : ReplayCandidateReason(ReplayCandidateReasonType.ProjectionDefinitionChanged);