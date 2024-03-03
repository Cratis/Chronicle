// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when event types have changed.
/// </summary>
public record ProjectionDefinitionChangedReplayCandidateReason()
    : ReplayCandidateReason(ReplayCandidateReasonType.ProjectionDefinitionChanged);
